using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Editing;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Services.Views;
using DataOrganizer.Templates;
using DataOrganizer.ViewModels;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using Material.Icons;
using NSubstitute;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadWindow)}"" type")]
internal class NotepadWindowTests
{
	#region Methods
	/// <summary>
	/// <see cref="NotepadViewModel.SelectedTab" />: a click on a tab from the text of another one gives the focus to its
	/// text.
	/// </summary>
	[AvaloniaTest]
	public async Task Click_On_A_Tab_Gives_The_Focus_To_Its_Text()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<NotepadTabView>(Arg.Any<object[]>())
				.Returns(x => new NotepadTabView((NotepadTabViewModel)x.Arg<object[]>()[0]));

			builder.RegisterInstance(viewFactory);
		});

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		// The application adds the template once its services are built, which the tests do without.
		sut
			.DataTemplates
			.Add(mock.Create<DocumentTabTemplate>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TextArea secondText = GetShownTextArea(sut);

		sut.ViewModel.SelectedTab = sut.ViewModel.Tabs[0];

		Dispatcher.UIThread.RunJobs();

		// The editor of the second tab was shown earlier, so its own first focus is behind it as well.
		await WaitForFirstFocus(GetShownTextArea(sut));

		Point point = Center(sut, GetHeaderText(sut.Tabs, sut.ViewModel.Tabs[1].Header));

		// Act
		Click(sut, point, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		secondText.IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.SelectedTab" />: a click on a tab selects its tab in the view model.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_A_Tab_Selects_It()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, GetHeaderText(sut.Tabs, sut.ViewModel.Tabs[0].Header));

		// Act
		Click(sut, point, MouseButton.Left);

		// Assert
		sut.ViewModel.SelectedTab
			.Should()
			.BeSameAs(sut.ViewModel.Tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.AddTabCommand" />: a click on the add button opens a new tab and selects it.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Add_Button_Adds_A_Selected_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, GetAddButton(sut.Tabs));

		// Act
		Click(sut, point, MouseButton.Left);

		// Assert
		sut.Tabs.ItemCount
			.Should()
			.Be(2);

		sut.Tabs.SelectedItem
			.Should()
			.BeSameAs(sut.ViewModel.Tabs[1]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CloseTabCommand" />: a click on the close button of a tab closes the tab.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Close_Button_Closes_The_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, GetCloseButton(sut.Tabs, 0));

		// Act
		Click(sut, point, MouseButton.Left);

		// Assert
		sut.ViewModel.Tabs.Select(x => x.Number)
			.Should()
			.Equal(2);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.ActivateMainWindowCommand" />: a click on the home button of the title bar activates
	/// the main window.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Home_Button_Activates_The_Main_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.HomeButton);

		// Act
		Click(sut, point, MouseButton.Left);

		// Assert
		viewLauncher
			.Received(1)
			.ActivateMainWindow();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.RenameTabCommand" />: the rename item of the menu of a tab renames that tab.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Rename_Menu_Item_Renames_Its_Tab()
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

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Click(sut, Center(sut, GetHeaderText(sut.Tabs, sut.ViewModel.Tabs[0].Header)), MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		FlyoutButton item = sut
			.GetVisualDescendants()
			.OfType<FlyoutButton>()
			.Single(x => x.Icon == MaterialIconKind.FormTextbox);

		Point point = Center(sut, item);

		// Act
		Click(sut, point, MouseButton.Left);

		// Assert
		sut.ViewModel.Tabs.Select(x => x.Name)
			.Should()
			.Equal("Notes", null);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.PreviousTab" />: after the saved tabs open, Ctrl+Tab goes to the tab kept for the
	/// session.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_After_The_Saved_Tabs_Open_Goes_To_The_Previous_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			NotepadSessionState sessionState = new()
			{
				PreviousTabNumber = 2
			};

			builder.RegisterInstance<INotepadSessionState>(sessionState);
		});

		NotepadViewModel viewModel = mock.Create<NotepadViewModel>();

		// The tabs open before the window, as when the notepad opens.
		viewModel.RestoreTabs(new()
		{
			SelectedTabNumber = 1,
			Tabs = [new(3), new(1), new(2)]
		});

		NotepadWindow sut = new(viewModel);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		sut.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		// Assert
		sut.ViewModel.SelectedTab
			.Should()
			.BeSameAs(sut.ViewModel.Tabs[2]);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.SelectedTab" />: Ctrl+Tab from the text of a tab gives the focus to the text of the tab
	/// it goes to.
	/// </summary>
	[AvaloniaTest]
	public async Task CtrlTab_Gives_The_Focus_To_The_Text_Of_The_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<NotepadTabView>(Arg.Any<object[]>())
				.Returns(x => new NotepadTabView((NotepadTabViewModel)x.Arg<object[]>()[0]));

			builder.RegisterInstance(viewFactory);
		});

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		// The application adds the template once its services are built, which the tests do without.
		sut
			.DataTemplates
			.Add(mock.Create<DocumentTabTemplate>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		TextArea secondText = GetShownTextArea(sut);

		sut.ViewModel.SelectedTab = sut.ViewModel.Tabs[0];

		Dispatcher.UIThread.RunJobs();

		// The editor of the second tab was shown earlier, so its own first focus is behind it as well.
		await WaitForFirstFocus(GetShownTextArea(sut));

		// Act
		sut.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		sut.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		Dispatcher.UIThread.RunJobs();

		// Assert
		secondText.IsFocused
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="NotepadViewModel.CenterMainWindowCommand" />: a double click on the home button of the title bar brings
	/// the main window to the screen of the notepad.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_On_The_Home_Button_Centers_The_Main_Window()
	{
		// Arrange
		IViewLauncher viewLauncher = Substitute.For<IViewLauncher>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(viewLauncher));

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(sut, sut.HomeButton);

		// Act
		Click(sut, point, MouseButton.Left);

		Click(sut, point, MouseButton.Left);

		// Assert
		viewLauncher
			.Received(1)
			.CenterMainWindow(sut);
	}

	/// <summary>
	/// <see cref="NotepadViewModel.PreviousTab" />: a selected tab leaves the tab selected before it as the previous one.
	/// </summary>
	[AvaloniaTest]
	public void PreviousTab_Follows_The_Selected_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.ViewModel.SelectedTab = sut.ViewModel.Tabs[0];

		// Assert
		sut.ViewModel.PreviousTab
			.Should()
			.BeSameAs(sut.ViewModel.Tabs[1]);
	}

	/// <summary>
	/// <see cref="NotepadWindow" />: the notepad shows its messages over the tabs in a snackbar host of its own, apart from
	/// the one of the main window.
	/// </summary>
	[AvaloniaTest]
	public void SnackbarHost_Is_The_One_Of_The_Notepad()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		// Act
		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		// Assert
		sut.SnackbarHost.HostName
			.Should()
			.Be(SnackbarHostIdentifiers.Notepad);

		sut.Tabs.GetLogicalAncestors()
			.Should()
			.Contain(sut.SnackbarHost);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.Name" />: a name given to an open tab shows in its header at once.
	/// </summary>
	[AvaloniaTest]
	public void Tabs_Show_A_New_Name_Of_Their_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.ViewModel.Tabs[0].Name = "Notes";

		// Assert
		IEnumerable<string?> texts = sut
			.Tabs
			.ContainerFromIndex(0)!
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Select(x => x.Text);

		texts
			.Should()
			.Contain("Notes");
	}

	/// <summary>
	/// <see cref="NotepadViewModel.SelectedTab" />: the window shows the editor of the selected tab, which the template of
	/// the document tabs builds.
	/// </summary>
	[AvaloniaTest]
	public void Tabs_Show_The_Editor_Of_The_Selected_Tab()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IViewFactory viewFactory = Substitute.For<IViewFactory>();

			viewFactory
				.CreateUserControl<NotepadTabView>(Arg.Any<object[]>())
				.Returns(x => new NotepadTabView((NotepadTabViewModel)x.Arg<object[]>()[0]));

			builder.RegisterInstance(viewFactory);
		});

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		// The application adds the template once its services are built, which the tests do without.
		sut
			.DataTemplates
			.Add(mock.Create<DocumentTabTemplate>());

		sut
			.ViewModel
			.AddTabCommand
			.Execute(null);

		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.ViewModel.SelectedTab = sut.ViewModel.Tabs[0];

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.Tabs.GetVisualDescendants().OfType<NotepadTabView>().Single().DataContext
			.Should()
			.BeSameAs(sut.ViewModel.Tabs[0]);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.Header" />: a tab of the window shows the header of its tab.
	/// </summary>
	[AvaloniaTest]
	public void Tabs_Show_Their_Headers()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		// Act
		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		IEnumerable<string?> texts = sut
			.Tabs
			.ContainerFromIndex(0)!
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Select(x => x.Text);

		texts
			.Should()
			.Contain(sut.ViewModel.Tabs[0].Header);
	}

	/// <summary>
	/// <see cref="NotepadTabViewModel.Header" />: a long header is cut at the width of a tab header and shown whole in a tip.
	/// </summary>
	[AvaloniaTest]
	public void Tabs_Trim_A_Long_Header_With_A_Tip()
	{
		// Arrange
		string name = RandomString.Create(200);

		using AutoMock mock = AutoMock.GetLoose();

		NotepadWindow sut = new(mock.Create<NotepadViewModel>());

		sut.ViewModel.Tabs[0].Name = name;

		// Act
		sut.Show();

		Dispatcher.UIThread.RunJobs();

		// Assert
		ToolTip.GetTip(GetHeaderText(sut.Tabs, name))
			.Should()
			.Be(name);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the point in the middle of the control.
	/// </summary>
	private static Point Center(Visual root, Visual target)
	{
		return target.TranslatePoint(
			new(
				target.Bounds.Width / 2.0,
				target.Bounds.Height / 2.0),
			root) ?? default;
	}

	/// <summary>
	/// Presses and releases a button of the mouse at a point of a window.
	/// </summary>
	private static void Click(Window window, Point point, MouseButton button)
	{
		window.MouseDown(point, button);

		window.MouseUp(point, button);
	}

	/// <summary>
	/// Returns the button that adds a tab.
	/// </summary>
	private static Button GetAddButton(DocumentTabControl control)
	{
		return control
			.GetVisualDescendants()
			.OfType<Button>()
			.Single(x => x.Name == "PART_AddButton");
	}

	/// <summary>
	/// Returns the close button of a tab.
	/// </summary>
	private static Button GetCloseButton(DocumentTabControl control, int index)
	{
		return control
			.ContainerFromIndex(index)!
			.GetVisualDescendants()
			.OfType<Button>()
			.Single();
	}

	/// <summary>
	/// Returns the text block of the tab header that shows the text.
	/// </summary>
	private static TextBlock GetHeaderText(DocumentTabControl control, string text)
	{
		return control
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single(x => x.Text == text && x.FindAncestorOfType<TabItem>() is not null);
	}

	/// <summary>
	/// Returns the text area of the upper half of the editor the tabs of the window show.
	/// </summary>
	private static TextArea GetShownTextArea(NotepadWindow window)
	{
		return window
			.Tabs
			.GetVisualDescendants()
			.OfType<NotepadTabView>()
			.Single()
			.Editor
			.Editor
			.PrimaryEditor
			.TextArea;
	}

	/// <summary>
	/// Waits for the focus an editor gives its text a moment after it is first loaded, so that it cannot come later.
	/// </summary>
	private static ValueTask<bool> WaitForFirstFocus(TextArea area)
	{
		Func<bool> isFocused = () =>
		{
			// The focus comes on a timer, which posts it to the UI thread.
			Dispatcher.UIThread.RunJobs();

			return area.IsFocused;
		};

		return isFocused.WaitAsync(millisecondsDelay: 10, maxRepeats: 1000);
	}
	#endregion
}
