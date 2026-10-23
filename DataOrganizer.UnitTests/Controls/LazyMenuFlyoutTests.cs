using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Controls;

namespace DataOrganizer.UnitTests.Controls;

[TestFixture(Description = $@"Tests of ""{nameof(LazyMenuFlyout)}"" type")]
internal class LazyMenuFlyoutTests
{
	#region Methods
	/// <summary>
	/// <see cref="LazyMenuFlyout.CreatePresenter" />: the presenter of an open menu shows to UI Automation as a menu
	/// element.
	/// </summary>
	[AvaloniaTest]
	public void CreatePresenter_Shows_The_Menu_To_UI_Automation_As_A_Menu()
	{
		// Arrange
		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock())
		};

		Border target = new();

		Show(target);

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		FlyoutPresenter presenter = ((Control)sut.Content!).FindLogicalAncestorOfType<FlyoutPresenter>()!;

		// Act
		AutomationPeer peer = ControlAutomationPeer.CreatePeerForElement(presenter);

		// Assert
		peer.GetAutomationControlType()
			.Should()
			.Be(AutomationControlType.Menu);

		peer.IsControlElement()
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="LazyMenuFlyout" />: Esc in a submenu closes the submenu alone, and the next Esc closes the menu it was
	/// opened from, as in the menus of Windows.
	/// </summary>
	[AvaloniaTest]
	public void Escape_After_A_Submenu_Closes_The_Menu()
	{
		// Arrange
		LazyMenuFlyout submenu = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => new FlyoutButton())
		};

		FlyoutButton opener = new()
		{
			Flyout = submenu
		};

		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => opener)
		};

		Border target = new();

		Window window = Show(target);

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		submenu.ShowAt(opener);

		Dispatcher.UIThread.RunJobs();

		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		bool[] afterFirst = [submenu.IsOpen, sut.IsOpen];

		// Act
		window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);

		Dispatcher.UIThread.RunJobs();

		// Assert
		afterFirst
			.Should()
			.Equal(false, true);

		sut.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FlyoutBase.Hide" />: the menu built from <see cref="LazyMenuFlyout.MenuTemplate" /> goes away with the
	/// flyout.
	/// </summary>
	[AvaloniaTest]
	public void Hide_Drops_The_Built_Menu()
	{
		// Arrange
		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock())
		};

		Border target = new();

		Show(target);

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.Hide();

		// Assert
		sut.Content
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FlyoutBase.Hide" />: a menu opened from the window, not from another menu, gives the focus to nobody.
	/// </summary>
	[AvaloniaTest]
	public void Hide_Gives_The_Target_Outside_A_Menu_No_Focus()
	{
		// Arrange
		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock())
		};

		TextBox input = new();

		Button target = new();

		Window window = Show(new StackPanel
		{
			Children =
			{
				input,
				target
			},
			Orientation = Orientation.Vertical
		});

		input.Focus();

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.Hide();

		Dispatcher.UIThread.RunJobs();

		// Assert
		window.FocusManager?.GetFocusedElement()
			.Should()
			.NotBeSameAs(target);
	}

	/// <summary>
	/// <see cref="FlyoutBase.Hide" />: without <see cref="LazyMenuFlyout.MenuTemplate" /> the content set by hand stays, as
	/// in a <see cref="Flyout" />.
	/// </summary>
	[AvaloniaTest]
	public void Hide_Keeps_The_Content_Without_A_MenuTemplate()
	{
		// Arrange
		TextBlock content = new();

		LazyMenuFlyout sut = new()
		{
			Content = content
		};

		Border target = new();

		Show(target);

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		// Act
		sut.Hide();

		// Assert
		sut.Content
			.Should()
			.BeSameAs(content);
	}

	/// <summary>
	/// <see cref="LazyMenuFlyout.MenuTemplate" />: every opening builds the menu anew, for the data the target has then.
	/// </summary>
	[AvaloniaTest]
	public void MenuTemplate_Builds_The_Menu_For_The_Current_Data_On_Every_Opening()
	{
		// Arrange
		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<string>((x, _) => new TextBlock
			{
				Text = x
			})
		};

		Border target = new()
		{
			DataContext = "first"
		};

		Show(target);

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		sut.Hide();

		target.DataContext = "second";

		// Act
		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		// Assert
		((TextBlock)sut.Content!).Text
			.Should()
			.Be("second");
	}

	/// <summary>
	/// <see cref="LazyMenuFlyout.MenuTemplate" />: an open menu keeps the data it was built for while the target takes
	/// other data.
	/// </summary>
	[AvaloniaTest]
	public void MenuTemplate_Keeps_The_Data_Of_An_Open_Menu()
	{
		// Arrange
		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock())
		};

		Border target = new()
		{
			DataContext = "first"
		};

		Show(target);

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		// Act
		target.DataContext = "second";

		// Assert
		((Control)sut.Content!).DataContext
			.Should()
			.Be("first");
	}

	/// <summary>
	/// <see cref="PopupFlyoutBase.OverlayDismissEventPassThrough" />: the press outside the menu that closes it still
	/// reaches the control under it.
	/// </summary>
	[AvaloniaTest]
	public void OverlayDismissEventPassThrough_Passes_The_Closing_Press_On()
	{
		// Arrange
		LazyMenuFlyout sut = new()
		{
			MenuTemplate = new FuncDataTemplate<object>((_, _) => new TextBlock
			{
				Text = "Item"
			})
		};

		Border target = new()
		{
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Border under = new()
		{
			Background = Brushes.Transparent,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Right,
			VerticalAlignment = VerticalAlignment.Bottom,
			Width = 50.0
		};

		int presses = 0;

		under.PointerPressed += (_, _) => presses++;

		Window window = Show(new Grid
		{
			Children =
			{
				target,
				under
			}
		});

		sut.ShowAt(target);

		Dispatcher.UIThread.RunJobs();

		Point point = Center(window, under);

		// Act
		window.MouseDown(point, MouseButton.Left);

		window.MouseUp(point, MouseButton.Left);

		// Assert
		presses
			.Should()
			.Be(1);
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
