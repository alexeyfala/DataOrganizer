using Autofac;
using Autofac.Extras.Moq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using DataOrganizer.Interfaces.Views;
using DataOrganizer.Models.Notepad;
using DataOrganizer.Services.Views;
using DataOrganizer.ViewModels.Windows;
using DataOrganizer.Windows;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Windows;

[TestFixture(Description = $@"Tests of ""{nameof(NotepadWindow)}"" type")]
internal class NotepadWindowTests
{
	#region Methods
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
		Click(sut, point);

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
		Click(sut, point);

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
		Click(sut, point);

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
		Click(sut, point);

		// Assert
		viewLauncher
			.Received(1)
			.ActivateMainWindow();
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
			TabNumbers = [3, 1, 2]
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
		Click(sut, point);

		Click(sut, point);

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
	/// <see cref="NotepadTab.Header" />: a tab of the window shows the header of its tab.
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
	/// Presses and releases the left button of the mouse at a point of a window.
	/// </summary>
	private static void Click(Window window, Point point)
	{
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);
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
	#endregion
}
