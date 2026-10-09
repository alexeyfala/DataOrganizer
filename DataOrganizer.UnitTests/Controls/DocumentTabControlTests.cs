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
using System;
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
	/// <see cref="DocumentTabControl.AdditionalMenuItemsTemplate" />: without items of a place the menu of a tab has no
	/// separator.
	/// </summary>
	[AvaloniaTest]
	public void AdditionalMenuItemsTemplate_Not_Set_Leaves_The_Menu_Without_A_Separator()
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
		IEnumerable<Separator> separators = GetMenu(window)
			.GetVisualDescendants()
			.OfType<Separator>()
			.Where(x => x.IsEffectivelyVisible);

		separators
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DocumentTabControl.AdditionalMenuItemsTemplate" />: a separator sets the items of a place apart from the
	/// common items of the menu of a tab.
	/// </summary>
	[AvaloniaTest]
	public void AdditionalMenuItemsTemplate_Sets_Its_Items_Apart_With_A_Separator()
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
		IEnumerable<Type> parts = GetMenu(window)
			.GetVisualDescendants()
			.Where(x => x.IsEffectivelyVisible && x is FlyoutButton or Separator)
			.Select(x => x.GetType());

		parts
			.Should()
			.Equal(
				typeof(FlyoutButton),
				typeof(FlyoutButton),
				typeof(FlyoutButton),
				typeof(Separator),
				typeof(FlyoutButton));
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
	/// <see cref="SelectingItemsControl.SelectedItem" />: the selected tab closed from its menu gives the focus to the tab
	/// that takes its place.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_A_Menu_Item_Gives_The_Focus_To_The_Tab_In_Place_Of_The_Closed_One()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second"];

		DocumentTabControl sut = new()
		{
			CloseCommand = new RelayCommand<string>(x => items.Remove(x!)),
			ItemsSource = items
		};

		Window window = Show(sut);

		Click(window, Center(window, GetHeaderText(sut, "first")), MouseButton.Right);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, GetMenuButtons(window)[0]);

		// Act
		Click(window, point, MouseButton.Left);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.ContainerFromIndex(0)!.IsFocused
			.Should()
			.BeTrue();
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
	/// <see cref="SelectingItemsControl.SelectedItem" />: after a tab is dragged, Ctrl+Tab goes back to the tab selected
	/// before it.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_After_A_Drag_Returns_To_The_Tab_Selected_Before()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		sut.SelectedIndex = 1;

		Drag(window, Center(window, GetHeaderText(sut, "second")), Center(window, GetHeaderText(sut, "third")));

		Dispatcher.UIThread.RunJobs();

		// Act
		PressCtrlTab(window);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("first");
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
	/// <see cref="DocumentTabControl.PreviousItem" />: Ctrl+Tab goes to the item set from outside.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_Goes_To_A_Previous_Item_Set_From_Outside()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		sut.PreviousItem = "third";

		// Act
		PressCtrlTab(window);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("third");
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
	/// <see cref="SelectingItemsControl.SelectedItem" />: Ctrl+Tab from a tab moves the focus to the tab it selects, whose
	/// content takes none.
	/// </summary>
	[AvaloniaTest]
	public void CtrlTab_Moves_The_Focus_To_The_Tab_It_Selects()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second"])
		};

		Window window = Show(sut);

		sut.PreviousItem = "second";

		// Act
		PressCtrlTab(window);

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.ContainerFromIndex(1)!.IsFocused
			.Should()
			.BeTrue();
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
	/// <see cref="ItemsControl.ItemsSource" />: a click that shakes the mouse by a few pixels does not move the tab.
	/// </summary>
	[AvaloniaTest]
	public void Drag_By_A_Few_Pixels_Keeps_The_Order()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Window window = Show(sut);

		Point point = Center(window, GetHeaderText(sut, "first"));

		// Act
		Drag(window, point, point + new Vector(3.0, 0.0));

		// Assert
		items
			.Should()
			.Equal("first", "second", "third");
	}

	/// <summary>
	/// <see cref="ItemsControl.ItemsSource" />: a drag that starts on the close button of a tab does not move the tab.
	/// </summary>
	[AvaloniaTest]
	public void Drag_From_The_Close_Button_Keeps_The_Order()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		// A disabled close button lets the press through to its tab.
		ICommand command = Substitute.For<ICommand>();

		command
			.CanExecute(Arg.Any<object?>())
			.Returns(true);

		DocumentTabControl sut = new()
		{
			CloseCommand = command,
			ItemsSource = items
		};

		Window window = Show(sut);

		Point from = Center(window, GetCloseButton(sut, 0));

		Point to = Center(window, GetHeaderText(sut, "third"));

		// Act
		Drag(window, from, to);

		// Assert
		items
			.Should()
			.Equal("first", "second", "third");
	}

	/// <summary>
	/// <see cref="TabControl.ContentTemplate" />: a dragged tab keeps its content, with no content built on the way, its own
	/// or of another tab.
	/// </summary>
	[AvaloniaTest]
	[TestCase("first", "second")]
	[TestCase("third", "second")]
	public void Drag_Keeps_The_Content_Of_The_Tab(string dragged, string target)
	{
		// Arrange
		List<string?> built = [];

		DocumentTabControl sut = new()
		{
			ContentTemplate = new FuncDataTemplate<string?>((x, _) =>
			{
				built.Add(x);

				return new Border();
			}),
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		sut.SelectedItem = dragged;

		Point from = Center(window, GetHeaderText(sut, dragged));

		Point to = Center(window, GetHeaderText(sut, target));

		built.Clear();

		// Act
		Drag(window, from, to);

		// Assert
		built
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ItemsControl.ItemsSource" />: a tab dragged to the left stays there while the pointer moves on before
	/// the row is laid out.
	/// </summary>
	[AvaloniaTest]
	public void Drag_Keeps_The_Order_Over_Moves_Faster_Than_The_Layout()
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Window window = Show(sut);

		Point from = Center(window, GetHeaderText(sut, "third"));

		Point to = Center(window, GetHeaderText(sut, "second"));

		window.MouseDown(from, MouseButton.Left);

		// Act
		RaiseMove(sut, to);

		RaiseMove(sut, to);

		window.MouseUp(to, MouseButton.Left);

		// Assert
		items
			.Should()
			.Equal("first", "third", "second");
	}

	/// <summary>
	/// <see cref="SelectingItemsControl.SelectedItem" />: the dragged tab stays selected in its own container, with no
	/// change of the selection on the way.
	/// </summary>
	[AvaloniaTest]
	public void Drag_Keeps_The_Tab_Selected_In_Its_Container()
	{
		// Arrange
		List<object?> selected = [];

		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Window window = Show(sut);

		Control container = sut.ContainerFromIndex(0)!;

		Point from = Center(window, GetHeaderText(sut, "first"));

		Point to = Center(window, GetHeaderText(sut, "third"));

		sut.SelectionChanged += (_, e) => selected.AddRange(e.AddedItems.Cast<object?>());

		// Act
		Drag(window, from, to);

		// Assert
		sut.SelectedItem
			.Should()
			.Be("first");

		sut.ContainerFromItem("first")
			.Should()
			.BeSameAs(container);

		selected
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ItemsControl.ItemsSource" />: a tab dragged onto another one takes its place among the items.
	/// </summary>
	[AvaloniaTest]
	[TestCase("first", "second", "second", "first", "third")]
	[TestCase("first", "third", "second", "third", "first")]
	[TestCase("third", "second", "first", "third", "second")]
	[TestCase("third", "first", "third", "first", "second")]
	public void Drag_Moves_The_Tab_To_Where_It_Is_Dropped(string dragged, string target, params string[] expected)
	{
		// Arrange
		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ItemsSource = items
		};

		Window window = Show(sut);

		Point from = Center(window, GetHeaderText(sut, dragged));

		Point to = Center(window, GetHeaderText(sut, target));

		// Act
		Drag(window, from, to);

		// Assert
		items
			.Should()
			.Equal(expected);
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
	/// <see cref="DocumentTabControl.PreviousItem" />: a selection keeps the item it leaves.
	/// </summary>
	[AvaloniaTest]
	public void PreviousItem_Follows_The_Selection()
	{
		// Arrange
		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"])
		};

		Show(sut);

		// Act
		sut.SelectedIndex = 1;

		// Assert
		sut.PreviousItem
			.Should()
			.Be("first");
	}

	/// <summary>
	/// <see cref="DocumentTabControl.PreviousItem" />: the selections made before the tabs show keep the item set from
	/// outside.
	/// </summary>
	[AvaloniaTest]
	public void PreviousItem_Ignores_The_Selections_Before_The_Tabs_Show()
	{
		// Arrange
		// A hidden control is not laid out, as the tabs a window loads before it shows them.
		DocumentTabControl sut = new()
		{
			IsVisible = false,
			ItemsSource = new ObservableCollection<string>(["first", "second", "third"]),
			PreviousItem = "second"
		};

		Show(sut);

		// Act
		sut.SelectedIndex = 2;

		sut.IsVisible = true;

		Dispatcher.UIThread.RunJobs();

		// Assert
		sut.PreviousItem
			.Should()
			.Be("second");
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
	/// <see cref="TabControl.ContentTemplate" />: the selected tab keeps its content while a tab before it closes, with no
	/// content of another tab built on the way.
	/// </summary>
	[AvaloniaTest]
	public void SelectedItem_Keeps_Its_Content_When_A_Tab_Before_It_Closes()
	{
		// Arrange
		List<string?> built = [];

		ObservableCollection<string> items = ["first", "second", "third"];

		DocumentTabControl sut = new()
		{
			ContentTemplate = new FuncDataTemplate<string?>((x, _) =>
			{
				built.Add(x);

				return new Border();
			}),
			ItemsSource = items
		};

		Show(sut);

		sut.SelectedIndex = 2;

		built.Clear();

		// Act
		items.Remove("first");

		// Assert
		built
			.Should()
			.BeEmpty();
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
	/// <see cref="SelectingItemsControl.SelectedItem" />: a tab selected while the focus is elsewhere in the window, as
	/// when a file is closed from the tree, leaves the focus there.
	/// </summary>
	[AvaloniaTest]
	public void SelectedItem_Set_While_The_Focus_Is_Elsewhere_Leaves_The_Focus_There()
	{
		// Arrange
		TextBox outside = new();

		DocumentTabControl sut = new()
		{
			ItemsSource = new ObservableCollection<string>(["first", "second"])
		};

		Show(new DockPanel
		{
			Children =
			{
				outside,
				sut
			}
		});

		outside.Focus();

		// Act
		sut.SelectedIndex = 1;

		Dispatcher.UIThread.RunJobs();

		// Assert
		outside.IsFocused
			.Should()
			.BeTrue();
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
	/// Presses the left button of the mouse at one point of a window, moves the mouse to another and releases it there.
	/// </summary>
	private static void Drag(Window window, Point from, Point to)
	{
		window.MouseDown(from, MouseButton.Left);

		window.MouseMove(to, RawInputModifiers.LeftMouseButton);

		window.MouseUp(to, MouseButton.Left);
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
	/// Raises a move of the mouse with the left button down on a control at a point of its window, with no layout pass
	/// before it, as when moves come faster than the layout.
	/// </summary>
	private static void RaiseMove(Control control, Point point)
	{
		control.RaiseEvent(new PointerEventArgs(
			InputElement.PointerMovedEvent,
			control,
			new Pointer(0, PointerType.Mouse, isPrimary: true),
			TopLevel.GetTopLevel(control),
			point,
			0,
			new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other),
			KeyModifiers.None));
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
