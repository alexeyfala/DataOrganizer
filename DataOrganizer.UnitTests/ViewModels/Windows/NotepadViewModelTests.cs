using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Views;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
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
	/// <see cref="NotepadViewModel(IDialogService, INotepadSessionState, IViewCache, IViewLauncher)" />: the notepad opens with one
	/// selected tab with the first number.
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
