using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AwesomeAssertions;
using DataOrganizer.Controls;
using Material.Icons;
using System.Linq;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(FlyoutButton)}"" type")]
internal class FlyoutButtonTests
{
	#region Methods
	/// <summary>
	/// <see cref="FlyoutButton" />: a click closes the flyout the button sits in, though no control owns the flyout.
	/// </summary>
	[AvaloniaTest]
	public void Click_Closes_A_Flyout_Shown_At_A_Control()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Header = "Item"
		};

		Flyout menu = new()
		{
			Content = sut
		};

		Border target = new();

		Window window = Show(target);

		menu.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, sut);

		// Act
		Click(window, point);

		// Assert
		menu.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FlyoutButton" />: a click closes the context flyout the button sits in.
	/// </summary>
	[AvaloniaTest]
	public void Click_Closes_The_Context_Flyout_Of_A_Control()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Header = "Item"
		};

		Flyout menu = new()
		{
			Content = sut
		};

		Border target = new()
		{
			ContextFlyout = menu
		};

		Window window = Show(target);

		menu.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, sut);

		// Act
		Click(window, point);

		// Assert
		menu.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FlyoutButton" />: a click on an item of a submenu closes the submenu and the menu above it.
	/// </summary>
	[AvaloniaTest]
	public void Click_In_A_Submenu_Closes_Every_Menu()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Header = "Item"
		};

		Flyout submenu = new()
		{
			Content = sut
		};

		FlyoutButton opener = new()
		{
			Flyout = submenu,
			Header = "More"
		};

		Flyout menu = new()
		{
			Content = opener
		};

		Border target = new();

		Window window = Show(target);

		menu.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		Click(window, Center(window, opener));

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, sut);

		// Act
		Click(window, point);

		// Assert
		submenu.IsOpen
			.Should()
			.BeFalse();

		menu.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FlyoutButton" />: a click leaves open a popup that is not a flyout.
	/// </summary>
	[AvaloniaTest]
	public void Click_Keeps_A_Popup_Open()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Header = "Item"
		};

		Popup popup = new()
		{
			Child = sut
		};

		Window window = Show(new Panel
		{
			Children =
			{
				popup
			}
		});

		popup.Open();

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, sut);

		// Act
		Click(window, point);

		// Assert
		popup.IsOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FlyoutButton" />: a click on a button that opens a submenu leaves the menu open.
	/// </summary>
	[AvaloniaTest]
	public void Click_On_A_Button_With_A_Submenu_Keeps_The_Menu_Open()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Flyout = new Flyout
			{
				Content = new FlyoutButton
				{
					Header = "Item"
				}
			},
			Header = "More"
		};

		Flyout menu = new()
		{
			Content = sut
		};

		Border target = new();

		Window window = Show(target);

		menu.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, sut);

		// Act
		Click(window, point);

		// Assert
		menu.IsOpen
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FlyoutButton.Gesture" />: a button without a header shows the keys in its tip, after the tip text.
	/// </summary>
	[AvaloniaTest]
	public void Gesture_Joins_The_Tip_Of_A_Button_Without_A_Header()
	{
		// Arrange
		FlyoutButton sut = new()
		{
			Content = "X",
			Gesture = "Ctrl+X"
		};

		ToolTip.SetTip(sut, "Cut");

		// Act
		Show(sut);

		// Assert
		ToolTip.GetTip(sut)
			.Should()
			.Be("Cut (Ctrl+X)");
	}

	/// <summary>
	/// <see cref="FlyoutButton.Gesture" />: the keys of the rows of a menu stand in one column at its right edge,
	/// whatever the length of the headers.
	/// </summary>
	[AvaloniaTest]
	public void Gesture_Lines_Up_At_The_Right_Edge()
	{
		// Arrange
		FlyoutButton shortRow = new()
		{
			Gesture = "F2",
			Header = "Next",
			Icon = MaterialIconKind.ArrowDown
		};

		FlyoutButton longRow = new()
		{
			Gesture = "Ctrl+Shift+U",
			Header = "A much longer header",
			Icon = MaterialIconKind.Magnify
		};

		// Act
		Show(new StackPanel
		{
			Orientation = Orientation.Vertical,
			Children =
			{
				shortRow,
				longRow
			}
		});

		// Assert
		GetKeysRight(shortRow)
			.Should()
			.BeApproximately(GetKeysRight(longRow), 1.0);
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
	/// Returns the right edge of the keys of a button in its own coordinates.
	/// </summary>
	private static double GetKeysRight(FlyoutButton button)
	{
		TextBlock keys = button
			.GetVisualDescendants()
			.OfType<TextBlock>()
			.Single(x => x.Text == button.Gesture);

		return keys.TranslatePoint(new(keys.Bounds.Width, 0.0), button)?.X ?? 0.0;
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
