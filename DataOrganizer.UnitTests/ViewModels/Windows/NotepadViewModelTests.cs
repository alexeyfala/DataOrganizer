using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Models.Notepad;
using DataOrganizer.Services.Views;
using DataOrganizer.ViewModels.Windows;
using NSubstitute;
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

		NotepadTab tab = sut.Tabs[0];

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

		NotepadTab tab = sut.Tabs[0];

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
	/// <see cref="NotepadViewModel(IDialogService, INotepadSessionState, IViewLauncher)" />: the notepad opens with one
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

		NotepadTab tab = sut.Tabs[0];

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

		NotepadTab tab = sut.Tabs[0];

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

		NotepadTab tab = sut.Tabs[0];

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
			TabNumbers = []
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
			TabNumbers = [3, 1, 2]
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
	/// <see cref="NotepadViewModel.RestoreTabs" />: Ctrl+Tab gets back the tab whose number is kept for the session.
	/// </summary>
	[Test]
	public void RestoreTabs_Restores_The_Previous_Tab_From_The_Session_State()
	{
		// Arrange
		NotepadViewSettings settings = new()
		{
			SelectedTabNumber = 1,
			TabNumbers = [3, 1, 2]
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
			TabNumbers = [3, 1, 2]
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
