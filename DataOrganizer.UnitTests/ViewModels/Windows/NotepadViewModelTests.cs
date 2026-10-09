using Autofac;
using Autofac.Extras.Moq;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Notepad;
using DataOrganizer.Interfaces.Notifications;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Notepad;
using DataOrganizer.UnitTests.Fakes;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.ViewModels.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadViewModel)}"" type")]
internal class NotepadViewModelTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadViewModel.AddTabCommand" />: the new tab comes after the open ones and gets selected.
	/// </summary>
	[Test]
	public void AddTabCommand_Adds_A_Selected_Tab_At_The_End()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut
			.AddTabCommand
			.Execute(null);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 2);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[1]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.AddTabCommand" />: a new tab leaves the number of a text that cannot be read alone, as
	/// its file stays on the disk.
	/// </summary>
	[Test]
	public async Task AddTabCommand_Skips_The_Number_Of_A_Text_That_Cannot_Be_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(2)
				.Returns((byte[]?)null);

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut.LoadTexts();

		await sut
			.CloseTabCommand
			.ExecuteAsync(sut.Tabs[1]);

		// Act
		sut
			.AddTabCommand
			.Execute(null);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 3);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.AddTabCommand" />: the new tab takes the smallest number no open tab has.
	/// </summary>
	[Test]
	public void AddTabCommand_Takes_The_Smallest_Free_Number()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.CloseTabCommand
			.Execute(sut.Tabs[1]);

		// Act
		sut
			.AddTabCommand
			.Execute(null);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 3, 2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a tab with text is closed only after a question in a dialog of the
	/// notepad that names the tab.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Asks_In_A_Dialog_Of_The_Notepad_Before_Closing_A_Tab_With_Text()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		tab.Document.Text = "Text";

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(tab);

		// Assert
		await dialogService.Received(1).RequestYesNoAsync(
			Arg.Any<string>(),
			Arg.Is(DialogHostIdentifiers.Notepad),
			Arg.Any<CancellationToken>());

		await dialogService.Received(1).RequestYesNoAsync(
			Arg.Is<string>(x => x.Contains(tab.Header)),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a tab with nothing but white space closes at once, with no question.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Closes_A_Tab_Of_White_Space_At_Once()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		tab.Document.Text = " \t\r\n";

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(tab);

		// Assert
		await dialogService.DidNotReceive().RequestYesNoAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());

		sut.Tabs
			.Should()
			.NotContain(tab);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a tab with text closes when the answer is yes.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Closes_A_Tab_With_Text_On_Consent()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestYesNoAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dialogService);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		tab.Document.Text = "Text";

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(tab);

		// Assert
		sut.Tabs
			.Should()
			.NotContain(tab);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the tab goes away and the others stay.
	/// </summary>
	[Test]
	public void CloseTabCommand_Closes_The_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		NotepadTabViewModel tab = sut.Tabs[0];

		// Act
		sut
			.CloseTabCommand
			.Execute(tab);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a text still waiting for a pause in typing is not written once its
	/// tab is closed, so the erased file does not come back.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Drops_The_Pending_Write_Of_The_Tab()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut.LoadTexts();

		// White space only, so the tab closes with no question.
		sut.Tabs[1].Document.Text = " ";

		await sut
			.CloseTabCommand
			.ExecuteAsync(sut.Tabs[1]);

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store
			.DidNotReceive()
			.Write(2, Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: before the texts are read from the disk, a closed tab erases
	/// nothing there.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Erases_Nothing_Before_The_Texts_Are_Read()
	{
		// Arrange
		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(store));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(sut.Tabs[1]);

		// Assert
		store
			.DidNotReceive()
			.Erase(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the text of a closed tab is erased from the disk.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Erases_The_Text_Of_The_Tab()
	{
		// Arrange
		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(store));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut.LoadTexts();

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(sut.Tabs[1]);

		// Assert
		store
			.Received(1)
			.Erase(2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a closed tab is no way back for Ctrl+Tab, in this window and the
	/// next one, since its number may go to a new tab.
	/// </summary>
	[Test]
	public void CloseTabCommand_Forgets_The_Previous_Tab()
	{
		// Arrange
		NotepadSessionState sessionState = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<INotepadSessionState>(sessionState));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut.PreviousTab = sut.Tabs[0];

		// Act
		sut
			.CloseTabCommand
			.Execute(sut.Tabs[0]);

		// Assert
		sut.PreviousTab
			.Should()
			.BeNull();

		sessionState.PreviousTabNumber
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a tab with text stays open when the answer is no.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Keeps_A_Tab_With_Text_On_A_Refusal()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestYesNoAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(dialogService);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		tab.Document.Text = "Text";

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(tab);

		// Assert
		sut.Tabs
			.Should()
			.Equal(tab);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the file of a text that cannot be read stays on the disk as it is.
	/// </summary>
	[Test]
	public async Task CloseTabCommand_Keeps_The_File_Of_A_Text_That_Cannot_Be_Read()
	{
		// Arrange
		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			store
				.Read(2)
				.Returns((byte[]?)null);

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut.LoadTexts();

		// Act
		await sut
			.CloseTabCommand
			.ExecuteAsync(sut.Tabs[1]);

		// Assert
		store
			.DidNotReceive()
			.Erase(Arg.Any<int>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the editor of a closed tab leaves the cache and gives up what it
	/// holds.
	/// </summary>
	[Test]
	public void CloseTabCommand_Removes_The_Editor_Of_The_Tab()
	{
		// Arrange
		IViewCache viewCache = Substitute.For<IViewCache>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewCache));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		NotepadTabViewModel tab = sut.Tabs[0];

		// Act
		sut
			.CloseTabCommand
			.Execute(tab);

		// Assert
		viewCache
			.Received(1)
			.Remove(tab);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: the last tab gives way to a new selected tab with the first number.
	/// </summary>
	[Test]
	public void CloseTabCommand_Replaces_The_Last_Tab_With_The_First_One()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.CloseTabCommand
			.Execute(sut.Tabs[0]);

		NotepadTabViewModel tab = sut.Tabs[0];

		// Act
		sut
			.CloseTabCommand
			.Execute(tab);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabsCommand" />: a group with text in several tabs is closed after one question in
	/// a dialog of the notepad.
	/// </summary>
	[Test]
	public async Task CloseTabsCommand_Asks_Once_In_A_Dialog_Of_The_Notepad_When_Tabs_Have_Text()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut.Tabs[0].Document.Text = "First";

		sut.Tabs[1].Document.Text = "Second";

		NotepadTabViewModel[] tabs = [.. sut.Tabs];

		// Act
		await sut
			.CloseTabsCommand
			.ExecuteAsync(tabs);

		// Assert
		await dialogService.Received(1).RequestYesNoAsync(
			Arg.Any<string>(),
			Arg.Is(DialogHostIdentifiers.Notepad),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabsCommand" />: a group of tabs without text closes at once, with no question.
	/// </summary>
	[Test]
	public async Task CloseTabsCommand_Closes_Blank_Tabs_At_Once()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.AddTabCommand
			.Execute(null);

		sut.Tabs[1].Document.Text = " \t\r\n";

		NotepadTabViewModel[] tabs = [sut.Tabs[0], sut.Tabs[1]];

		// Act
		await sut
			.CloseTabsCommand
			.ExecuteAsync(tabs);

		// Assert
		await dialogService.DidNotReceive().RequestYesNoAsync(
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());

		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(3);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabsCommand" />: the selected tab of the group closes last, so the others close
	/// without being shown on the way.
	/// </summary>
	[Test]
	public async Task CloseTabsCommand_Closes_The_Selected_Tab_Last()
	{
		// Arrange
		List<NotepadTabViewModel> closed = [];

		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.AddTabCommand
			.Execute(null);

		sut.SelectedTab = sut.Tabs[0];

		NotepadTabViewModel[] tabs = [sut.Tabs[0], sut.Tabs[1]];

		sut.Tabs.CollectionChanged += (_, e) => closed.AddRange(e.OldItems?.Cast<NotepadTabViewModel>() ?? []);

		// Act
		await sut
			.CloseTabsCommand
			.ExecuteAsync(tabs);

		// Assert
		closed
			.Should()
			.Equal(tabs[1], tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabsCommand" />: a group with text closes when the answer is yes, and the other
	/// tabs stay.
	/// </summary>
	[Test]
	public async Task CloseTabsCommand_Closes_The_Tabs_Of_The_Group_On_Consent()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestYesNoAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dialogService);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.AddTabCommand
			.Execute(null);

		sut.Tabs[1].Document.Text = "Text";

		NotepadTabViewModel[] tabs = [sut.Tabs[0], sut.Tabs[1]];

		// Act
		await sut
			.CloseTabsCommand
			.ExecuteAsync(tabs);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(3);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabsCommand" />: when the answer is no, every tab of the group stays open, the
	/// blank ones too.
	/// </summary>
	[Test]
	public async Task CloseTabsCommand_Keeps_Every_Tab_On_A_Refusal()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestYesNoAsync(
					Arg.Any<string>(),
					Arg.Any<string>(),
					Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(dialogService);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		sut
			.AddTabCommand
			.Execute(null);

		sut.Tabs[1].Document.Text = "Text";

		NotepadTabViewModel[] tabs = [sut.Tabs[0], sut.Tabs[1]];

		// Act
		await sut
			.CloseTabsCommand
			.ExecuteAsync(tabs);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 2, 3);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabsCommand" />: closing every tab leaves a new selected tab with the first number.
	/// </summary>
	[Test]
	public async Task CloseTabsCommand_Replaces_All_Tabs_With_The_First_One()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		NotepadTabViewModel[] tabs = [.. sut.Tabs];

		// Act
		await sut
			.CloseTabsCommand
			.ExecuteAsync(tabs);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel(IDialogService, IDispatcherAccessor, INotepadSessionState, INotepadStore, INotificationService, IViewCache, IViewLauncher, TimeProvider)" />:
	/// the notepad opens with one selected tab with the first number.
	/// </summary>
	[Test]
	public void Constructor_Opens_The_First_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1);

		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[0]);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: the editors of all tabs leave the cache, which the application
	/// keeps after the window.
	/// </summary>
	[Test]
	public void Dispose_Removes_The_Editors_Of_The_Tabs()
	{
		// Arrange
		IViewCache viewCache = Substitute.For<IViewCache>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewCache));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		// Act
		sut.Dispose();

		// Assert
		viewCache
			.Received(1)
			.Remove(sut.Tabs[0]);

		viewCache
			.Received(1)
			.Remove(sut.Tabs[1]);
	}

	/// <summary>
	/// <see cref="ObservableDisposableBase.Dispose" />: closing the notepad writes the texts that still wait for a pause in
	/// typing.
	/// </summary>
	[Test]
	public void Dispose_Writes_The_Texts_Waiting_For_A_Pause()
	{
		// Arrange
		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(store);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		sut.Tabs[0].Document.Text = "Text";

		// Act
		sut.Dispose();

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text"))));
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a text written in UTF-8 because its encoding could not hold it stays in
	/// UTF-8 from then on.
	/// </summary>
	[Test]
	public void LoadTexts_Keeps_A_Text_In_Utf8_Once_Its_Encoding_Could_Not_Hold_It()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			store
				.Read(1)
				.Returns(contents);

			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		TextDocument document = sut.Tabs[0].Document;

		// A lone surrogate, which UTF-16 cannot hold either.
		document.Insert(4, "\uD800");

		time.Advance(NotepadViewModel.WriteDelay);

		document.Text = "Text!";

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text!"))));
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a tab whose bytes are not text is shown read-only.
	/// </summary>
	[Test]
	public void LoadTexts_Makes_A_Tab_Read_Only_When_Its_Bytes_Are_Not_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			// A zero byte with an odd count of bytes, so neither UTF-16 nor any code page reads them.
			store
				.Read(1)
				.Returns([0x00, 0xFF, 0x00]);

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.LoadTexts();

		// Assert
		sut.Tabs[0].IsReadOnly
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a tab whose file cannot be read is shown read-only.
	/// </summary>
	[Test]
	public void LoadTexts_Makes_A_Tab_Read_Only_When_Its_File_Cannot_Be_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(1)
				.Returns((byte[]?)null);

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.LoadTexts();

		// Assert
		sut.Tabs[0].IsReadOnly
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: texts that no tab has, as after a crash, open in tabs at the end, in the
	/// order of their numbers.
	/// </summary>
	[Test]
	public void LoadTexts_Opens_A_Tab_At_The_End_For_Each_Text_Without_One()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.FindNumbers()
				.Returns([4, 1, 3]);

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.LoadTexts();

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1, 3, 4);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: each change puts the write off, so nothing is written while typing goes
	/// on.
	/// </summary>
	[Test]
	public void LoadTexts_Puts_A_Write_Off_While_Typing_Goes_On()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		TextDocument document = sut.Tabs[0].Document;

		document.Insert(0, "a");

		time.Advance(NotepadViewModel.WriteDelay / 2.0);

		document.Insert(1, "b");

		// Act
		time.Advance(NotepadViewModel.WriteDelay / 2.0);

		// Assert
		store
			.DidNotReceive()
			.Write(Arg.Any<int>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a text with a null character, which a guess of the encoding would take for
	/// binary data, is read in UTF-8.
	/// </summary>
	[Test]
	public void LoadTexts_Reads_A_Text_With_A_Null_Character()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("A\0B"));

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.LoadTexts();

		// Assert
		sut.Tabs[0].Document.Text
			.Should()
			.Be("A\0B");
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: every tab gets the text of its file.
	/// </summary>
	[Test]
	public void LoadTexts_Reads_The_Texts_Of_The_Tabs()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("First"));

			store
				.Read(2)
				.Returns(Encoding.UTF8.GetBytes("Second"));

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		// Act
		sut.LoadTexts();

		// Assert
		sut.Tabs.Select(x => x.Document.Text)
			.Should()
			.Equal("First", "Second");
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: undo starts from the text as it was read, not from an empty tab.
	/// </summary>
	[Test]
	public void LoadTexts_Starts_The_Undo_From_The_Read_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			INotepadStore store = Substitute.For<INotepadStore>();

			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("Text"));

			builder.RegisterInstance(store);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.LoadTexts();

		// Assert
		sut.Tabs[0].Document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a changed text is written to the disk once typing pauses.
	/// </summary>
	[Test]
	public void LoadTexts_Writes_A_Changed_Text_After_A_Pause_In_Typing()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		sut.Tabs[0].Document.Text = "Text";

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text"))));
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a changed text goes back to the disk in the encoding it was read in.
	/// </summary>
	[Test]
	public void LoadTexts_Writes_A_Text_Back_In_The_Encoding_It_Was_Read_In()
	{
		// Arrange
		byte[] expected = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text!")];

		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			store
				.Read(1)
				.Returns(contents);

			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		sut
			.Tabs[0]
			.Document
			.Insert(4, "!");

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(expected)));
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: a text that its encoding cannot hold is written in UTF-8, where a lone
	/// surrogate becomes the replacement character.
	/// </summary>
	[Test]
	public void LoadTexts_Writes_A_Text_That_Its_Encoding_Cannot_Hold_In_Utf8()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			byte[] contents = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes("Text")];

			store
				.Read(1)
				.Returns(contents);

			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		// A lone surrogate, which UTF-16 cannot hold either.
		sut
			.Tabs[0]
			.Document
			.Insert(4, "\uD800");

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store.Received(1).Write(
			1,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text�"))));
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: reading the texts is not a change, so nothing goes back to the disk
	/// until a text changes.
	/// </summary>
	[Test]
	public void LoadTexts_Writes_Nothing_Until_A_Text_Changes()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			store
				.Read(1)
				.Returns(Encoding.UTF8.GetBytes("Text"));

			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store
			.DidNotReceive()
			.Write(Arg.Any<int>(), Arg.Any<byte[]>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.LoadTexts" />: the text of a tab opened afterwards is written to the disk as well.
	/// </summary>
	[Test]
	public void LoadTexts_Writes_The_Texts_Of_New_Tabs()
	{
		// Arrange
		FakeTimeProvider time = new();

		INotepadStore store = Substitute.For<INotepadStore>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(store);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<InlineDispatcherAccessor>()
				.As<IDispatcherAccessor>();
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut.LoadTexts();

		sut
			.AddTabCommand
			.Execute(null);

		sut.Tabs[1].Document.Text = "Text";

		// Act
		time.Advance(NotepadViewModel.WriteDelay);

		// Assert
		store.Received(1).Write(
			2,
			Arg.Is<byte[]>(x => x.SequenceEqual(Encoding.UTF8.GetBytes("Text"))));
	}

	/// <summary>
	/// <see cref="NotepadViewModel.PreviousTab" />: the number of the tab is kept for the session.
	/// </summary>
	[Test]
	public void PreviousTab_Is_Written_To_The_Session_State()
	{
		// Arrange
		NotepadSessionState sessionState = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<INotepadSessionState>(sessionState));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		sut
			.AddTabCommand
			.Execute(null);

		// Act
		sut.PreviousTab = sut.Tabs[1];

		// Assert
		sessionState.PreviousTabNumber
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RenameTabCommand" />: the name is asked in a dialog of the notepad that starts from the
	/// header of the tab.
	/// </summary>
	[Test]
	public async Task RenameTabCommand_Asks_For_The_Name_In_A_Dialog_Of_The_Notepad()
	{
		// Arrange
		IDialogService dialogService = Substitute.For<IDialogService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dialogService));

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		// Act
		await sut
			.RenameTabCommand
			.ExecuteAsync(tab);

		// Assert
		await dialogService.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x.DialogHostIdentifier == DialogHostIdentifiers.Notepad),
			Arg.Any<CancellationToken>());

		await dialogService.Received(1).RequestKeyValueInputAsync(
			Arg.Is<KeyValueInputParameters>(x => x.Key == tab.Header),
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RenameTabCommand" />: the entered name goes to the tab.
	/// </summary>
	[Test]
	public async Task RenameTabCommand_Gives_The_Tab_The_Entered_Name()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestKeyValueInputAsync(
					Arg.Any<KeyValueInputParameters>(),
					Arg.Any<CancellationToken>())
				.Returns(new KeyValueInput("Notes"));

			builder.RegisterInstance(dialogService);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		// Act
		await sut
			.RenameTabCommand
			.ExecuteAsync(tab);

		// Assert
		tab.Name
			.Should()
			.Be("Notes");
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RenameTabCommand" />: a dialog closed without a name leaves the name of the tab.
	/// </summary>
	[Test]
	public async Task RenameTabCommand_Keeps_The_Name_When_The_Dialog_Is_Cancelled()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDialogService dialogService = Substitute.For<IDialogService>();

			dialogService
				.RequestKeyValueInputAsync(
					Arg.Any<KeyValueInputParameters>(),
					Arg.Any<CancellationToken>())
				.Returns((KeyValueInput?)null);

			builder.RegisterInstance(dialogService);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		NotepadTabViewModel tab = sut.Tabs[0];

		tab.Name = "Notes";

		// Act
		await sut
			.RenameTabCommand
			.ExecuteAsync(tab);

		// Assert
		tab.Name
			.Should()
			.Be("Notes");
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RestoreTabs" />: without saved tabs the first tab stays open.
	/// </summary>
	[Test]
	public void RestoreTabs_Keeps_The_First_Tab_Without_Saved_Tabs()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = null,
			Tabs = []
		};

		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.RestoreTabs(settings);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(1);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RestoreTabs" />: the saved tabs take the place of the open ones, in the saved order.
	/// </summary>
	[Test]
	public void RestoreTabs_Opens_The_Saved_Tabs_In_Their_Order()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 3,
			Tabs = [new(3), new(1), new(2)]
		};

		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.RestoreTabs(settings);

		// Assert
		sut.Tabs.Select(x => x.Number)
			.Should()
			.Equal(3, 1, 2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RestoreTabs" />: the saved tabs get their names back.
	/// </summary>
	[Test]
	public void RestoreTabs_Restores_The_Names_Of_The_Tabs()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 1,
			Tabs = [new(1, "Notes"), new(2)]
		};

		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.RestoreTabs(settings);

		// Assert
		sut.Tabs.Select(x => x.Name)
			.Should()
			.Equal("Notes", null);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RestoreTabs" />: Ctrl+Tab gets back the tab whose number is kept for the session.
	/// </summary>
	[Test]
	public void RestoreTabs_Restores_The_Previous_Tab_From_The_Session_State()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 1,
			Tabs = [new(3), new(1), new(2)]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadSessionState sessionState = new()
			{
				PreviousTabNumber = 3
			};

			builder.RegisterInstance<INotepadSessionState>(sessionState);
		});

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.RestoreTabs(settings);

		// Assert
		sut.PreviousTab
			.Should()
			.BeSameAs(sut.Tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RestoreTabs" />: the saved tab gets selected, or the first one when no tab has the saved
	/// number.
	/// </summary>
	[Test]
	[TestCase(1, 1)]
	[TestCase(7, 0)]
	public void RestoreTabs_Selects_The_Saved_Tab_Or_The_First_One(int saved, int expectedIndex)
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = saved,
			Tabs = [new(3), new(1), new(2)]
		};

		using AutoMock mock = AutoMock.GetLoose();

		NotepadViewModel sut = mock.Create<NotepadViewModel>();

		// Act
		sut.RestoreTabs(settings);

		// Assert
		sut.SelectedTab
			.Should()
			.BeSameAs(sut.Tabs[expectedIndex]);
	}
	#endregion
}
