using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.Controls;
using Material.Icons;
using NSubstitute;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(DocumentTabControl)}"" type")]
internal class DocumentTabControlTests
{
	#region Methods
	/// <summary>
	/// <see cref="DocumentTabControl.AddCommand" />: the add button stays in sight at the right edge when the tabs do not
	/// fit.
	/// </summary>
	[AvaloniaTest]
	public void AddCommand_Keeps_The_Add_Button_At_The_Right_Edge_When_The_Tabs_Overflow()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			AddCommand = Substitute.For<ICommand>(),
			ItemsSource = new ObservableCollection<string>(Enumerable.Range(1, 50).Select(x => $"tab {x}"))
		};

		// Act
		Show(sut);

		// Assert
		GetRight(sut, GetAddButton(sut))
			.Should()
			.BeApproximately(sut.Bounds.Width, 1.0);
	}

	/// <summary>
	/// <see cref="DocumentTabControl.AddCommand" />: the add button stands right after the last tab.
	/// </summary>
	[AvaloniaTest]
	public void AddCommand_Puts_The_Add_Button_Right_After_The_Last_Tab()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			AddCommand = Substitute.For<ICommand>(),
			ItemsSource = new ObservableCollection<string>(["first", "second"])
		};

		// Act
		Show(sut);

		// Assert
		GetLeft(sut, GetAddButton(sut))
			.Should()
			.BeApproximately(GetRight(sut, sut.ContainerFromIndex(1)!), 1.0);
	}

	/// <summary>
	/// <see cref="DocumentTabControl.AddCommand" />: the add button shows only while the command is set.
	/// </summary>
	[AvaloniaTest]
	[TestCase(false)]
	[TestCase(true)]
	public void AddCommand_Shows_The_Add_Button_Only_When_Set(bool isSet)
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			AddCommand = isSet ? Substitute.For<ICommand>() : null,
			ItemsSource = new ObservableCollection<string>(["first"])
		};

		// Act
		Show(sut);

		// Assert
		GetAddButton(sut).IsVisible
			.Should()
			.Be(isSet);
	}

	/// <summary>
	/// <see cref="DocumentTabControl.AdditionalMenuItemsTemplate" />: the items of a place come at the bottom of the menu
	/// of a tab and get the item of the tab.
	/// </summary>
	[AvaloniaTest]
	public void AdditionalMenuItemsTemplate_Adds_Items_At_The_Bottom_Of_The_Menu()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			AdditionalMenuItemsTemplate = new FuncDataTemplate<string>((x, _) => new FlyoutButton
			{
				Header = $"Rename {x}"
			}),
			ItemsSource = new ObservableCollection<string>(["first", "second"])
		};

		Window window = Show(sut);

		Point point = Center(window, GetHeaderText(sut, "second"));

		// Act
		Click(window, point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetMenuButtons(window)[^1].Header
			.Should()
			.Be("Rename second");
	}

	/// <summary>
	/// <see cref="DocumentTabControl.TabMenuTemplate" />: the items of the menu of a tab close the tab, the other tabs or
	/// all of them.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0, "first", "third")]
	[TestCase(1, "second")]
	[TestCase(2)]
	public void Click_On_A_Menu_Item_Closes_Its_Tabs(int index, params string[] expected)
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			CloseCommand = new RelayCommand<string>(x => items.Remove(x!)),
			ItemsSource = items
		};

		Window window = Show(sut);

		Click(window, Center(window, GetHeaderText(sut, "second")), MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, GetMenuButtons(window)[index]);

		// Act
		Click(window, point, MouseButton.Left);

		// Assert
		items
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="DocumentTabControl.TabMenuTemplate" />: a click on an item of the menu of a tab closes the menu, even
	/// when no tab goes away.
	/// </summary>
	[AvaloniaTest]
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	public void Click_On_A_Menu_Item_Closes_The_Menu(int index)
	{
		// Arrange
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		DocumentTabControl sut = new()
		{
			CloseCommand = command,
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		Click(window, Center(window, GetHeaderText(sut, "second")), MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, GetMenuButtons(window)[index]);

		// Act
		Click(window, point, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		window.GetVisualDescendants().OfType<FlyoutPresenter>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DocumentTabControl.AddCommand" />: the add button runs the command.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Add_Button_Runs_The_Add_Command()
	{
		// Arrange
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		DocumentTabControl sut = new()
		{
			AddCommand = command,
			ItemsSource = new ObservableCollection<string>(["first"])
		};

		Window window = Show(sut);

		Point point = Center(window, GetAddButton(sut));

		// Act
		Click(window, point, MouseButton.Left);

		// Assert
		command
			.Received(1)
			.Execute(null);
	}

	/// <summary>
	/// <see cref="DocumentTabControl.CloseCommand" />: the close button of a tab runs the command for the item of the tab.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_The_Close_Button_Runs_The_Close_Command()
	{
		// Arrange
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		DocumentTabControl sut = new()
		{
			CloseCommand = command,
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		Point point = Center(window, GetCloseButton(sut, 1));

		// Act
		Click(window, point, MouseButton.Left);

		// Assert
		command
			.Received(1)
			.Execute("second");
	}

	/// <summary>
	/// <see cref="DocumentTabControl.CloseOtherTabsCommand" />: cannot be executed while a single tab is open.
	/// </summary>
	[AvaloniaTest]
	public void CloseOtherTabsCommand_Is_Disabled_For_A_Single_Tab()
	{
		// Arrange
		ObservableCollection<string> items = ["first"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		// Act
		bool canExecuteWithSingleTab = sut.CloseOtherTabsCommand.CanExecute("first");

		items.Add("second");

		bool canExecuteWithSecondTab = sut.CloseOtherTabsCommand.CanExecute("first");

		// Assert
		canExecuteWithSingleTab
			.Should()
			.BeFalse();

		canExecuteWithSecondTab
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: after the selected tab is closed, Ctrl+Tab goes back to the tab
	/// selected before it.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_After_Closing_The_Selected_Tab_Returns_To_The_One_Before()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third", "fourth"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Window window = Show(sut);

		sut.SelectedIndex = 1;

		sut.SelectedIndex = 3;

		items.Remove("fourth");

		Dispatcher.UIThread.RunJobs();

		// Act
		PressCtrlTab(window);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("second");
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: Ctrl+Tab finds the tab selected before, whatever moved its index.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_Follows_The_Tab_Not_The_Index()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Window window = Show(sut);

		sut.SelectedIndex = 1;

		sut.SelectedIndex = 2;

		items.Remove("first");

		Dispatcher.UIThread.RunJobs();

		// Act
		PressCtrlTab(window);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("second");
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: Ctrl+Tab does nothing once the tab selected before is closed.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_Ignores_A_Closed_Tab()
	{
		// Arrange
		// Three tabs, so that a fall back to the first one differs from staying.
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Window window = Show(sut);

		sut.SelectedIndex = 2;

		items.Remove("first");

		Dispatcher.UIThread.RunJobs();

		// Act
		PressCtrlTab(window);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("third");
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: Ctrl+Tab goes back to the tab selected before the current one.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_Selects_The_Previously_Selected_Tab()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		sut.SelectedIndex = 1;

		sut.SelectedIndex = 2;

		Dispatcher.UIThread.RunJobs();

		// Act
		PressCtrlTab(window);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("second");
	}

	/// <summary>
	/// <see cref="DocumentTabControl.CloseCommand" />: a middle click on a tab runs the command for the item of the tab.
	/// </summary>
	[AvaloniaTest]
	public void MiddleClick_On_A_Tab_Runs_The_Close_Command()
	{
		// Arrange
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		DocumentTabControl sut = new()
		{
			CloseCommand = command,
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		Point point = Center(window, GetHeaderText(sut, "second"));

		// Act
		Click(window, point, MouseButton.Middle);

		// Assert
		command
			.Received(1)
			.Execute("second");
	}

	/// <summary>
	/// <see cref="DocumentTabControl.CloseCommand" />: a middle click on the content of a tab closes nothing.
	/// </summary>
	[AvaloniaTest]
	public void MiddleClick_On_The_Content_Keeps_The_Tab()
	{
		// Arrange
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		DocumentTabControl sut = new()
		{
			CloseCommand = command,
			ItemsSource = new ObservableCollection<string>(["first", "second"])
		};

		Window window = Show(sut);

		TextBlock content = sut
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single(x => x.Text == "first" && x.FindAncestorOfType<TabItem>() is null);

		Point point = Center(window, content);

		// Act
		Click(window, point, MouseButton.Middle);

		// Assert
		command
			.DidNotReceive()
			.Execute(Arg.Any<object?>());
	}

	/// <summary>
	/// <see cref="DocumentTabControl.TabMenuTemplate" />: a right click on a tab opens the menu that closes tabs, with
	/// nothing more when the place adds no items.
	/// </summary>
	[AvaloniaTest]
	public void RightClick_On_A_Tab_Opens_The_Common_Menu()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second"])
		};

		Window window = Show(sut);

		Point point = Center(window, GetHeaderText(sut, "second"));

		// Act
		Click(window, point, MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		// Assert
		GetMenuButtons(window).Select(x => x.Icon)
			.Should()
			.Equal(
				MaterialIconKind.Close,
				MaterialIconKind.CloseBoxMultipleOutline,
				MaterialIconKind.CloseBoxMultiple);

		IEnumerable<string?> texts = GetMenu(window)
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Where(x => x.IsEffectivelyVisible)
			.Select(x => x.Text);

		texts
			.Should()
			.NotContain("second");
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedIndex" />: a right click selects the tab, so that its menu acts on it.
	/// </summary>
	[AvaloniaTest]
	public void RightClick_On_A_Tab_Selects_It()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		Point point = Center(window, GetHeaderText(sut, "third"));

		// Act
		Click(window, point, MouseButton.Right);

		// Assert
		sut.SelectedIndex
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: the closed last tab gives way to its left neighbour.
	/// </summary>
	[AvaloniaTest]
	public void SelectedItem_Moves_To_The_Left_Neighbour_When_The_Last_Tab_Closes()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Show(sut);

		sut.SelectedIndex = 2;

		// Act
		items.Remove("third");

		// Assert
		sut.SelectedItem
			.Should()
			.Be("second");
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: the closed tab gives way to its right neighbour at once, with no
	/// stop at the first tab.
	/// </summary>
	[AvaloniaTest]
	public void SelectedItem_Moves_To_The_Right_Neighbour_When_The_Selected_Tab_Closes()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		List<object?> selected = [];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Show(sut);

		sut.SelectedIndex = 1;

		sut.SelectionChanged += (_, e) => selected.AddRange(e.AddedItems.Cast<object?>());

		// Act
		items.Remove("second");

		// Assert
		selected
			.Should()
			.Equal("third");
	}

	/// <summary>
	/// <see cref="DocumentTabControl.TabHeaderTemplate" />: the header of a tab shows what the item template builds.
	/// </summary>
	[AvaloniaTest]
	public void TabHeaderTemplate_Shows_The_Header_Built_By_The_Item_Template()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock
			{
				Text = x.ToUpperInvariant()
			}),
			ItemsSource = new ObservableCollection<string>(["first"])
		};

		// Act
		Show(sut);

		// Assert
		IEnumerable<string?> texts = sut
			.ContainerFromIndex(0)!
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Select(x => x.Text);

		texts
			.Should()
			.Contain("FIRST");
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
	/// Returns the left edge of the control in the coordinates of the root.
	/// </summary>
	private static double GetLeft(Visual root, Visual target)
	{
		return target.TranslatePoint(default, root)?.X ?? double.NaN;
	}

	/// <summary>
	/// Returns the open menu of a tab.
	/// </summary>
	private static FlyoutPresenter GetMenu(Window window)
	{
		return window
			.GetVisualDescendants()
			.OfType<FlyoutPresenter>()
			.Single();
	}

	/// <summary>
	/// Returns the visible buttons of the open menu of a tab, from top to bottom.
	/// </summary>
	private static FlyoutButton[] GetMenuButtons(Window window)
	{
		return [.. GetMenu(window)
			.GetVisualDescendants()
			.OfType<FlyoutButton>()
			.Where(x => x.IsEffectivelyVisible)];
	}

	/// <summary>
	/// Returns the right edge of the control in the coordinates of the root.
	/// </summary>
	private static double GetRight(Visual root, Visual target)
	{
		return target.TranslatePoint(new(target.Bounds.Width, 0.0), root)?.X ?? double.NaN;
	}

	/// <summary>
	/// Presses and releases Ctrl+Tab in a window.
	/// </summary>
	private static void PressCtrlTab(Window window)
	{
		window.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);

		window.KeyReleaseQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
	}

	/// <summary>
	/// Shows the content in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(Control content)
	{
		Window window = new()
		{
			Content = content,
			Height = 600.0,
			Width = 800.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
