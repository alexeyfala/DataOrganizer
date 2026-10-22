using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(ShowContextFlyoutOnDoubleTapBehavior)}"" type")]
internal class ShowContextFlyoutOnDoubleTapBehaviorTests
{
	#region Data
	/// <summary>
	/// A point over the target of the behavior.
	/// </summary>
	private static readonly Point Inside = new(25.0, 25.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ShowContextFlyoutOnDoubleTapBehavior" />: a single click leaves the context flyout closed.
	/// </summary>
	[AvaloniaTest]
	public void Click_Leaves_The_Context_Flyout_Closed()
	{
		// Arrange
		Flyout flyout = new()
		{
			Content = new TextBlock
			{
				Text = "Item"
			}
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			ContextFlyout = flyout,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = Show(target);

		ShowContextFlyoutOnDoubleTapBehavior sut = new();

		sut.Attach(target);

		// Act
		Click(window);

		// Assert
		flyout.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ShowContextFlyoutOnDoubleTapBehavior" />: once detached, the behavior opens nothing on a double click.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_After_Detach_Leaves_The_Context_Flyout_Closed()
	{
		// Arrange
		Flyout flyout = new()
		{
			Content = new TextBlock
			{
				Text = "Item"
			}
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			ContextFlyout = flyout,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = Show(target);

		ShowContextFlyoutOnDoubleTapBehavior sut = new();

		sut.Attach(target);

		sut.Detach();

		// Act
		Click(window);

		Click(window);

		// Assert
		flyout.IsOpen
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ShowContextFlyoutOnDoubleTapBehavior" />: a double click opens the context flyout of the control.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_Opens_The_Context_Flyout()
	{
		// Arrange
		Flyout flyout = new()
		{
			Content = new TextBlock
			{
				Text = "Item"
			}
		};

		Border target = new()
		{
			Background = Brushes.Transparent,
			ContextFlyout = flyout,
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = Show(target);

		ShowContextFlyoutOnDoubleTapBehavior sut = new();

		sut.Attach(target);

		// Act
		Click(window);

		Click(window);

		// Assert
		flyout.IsOpen
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Presses and releases the left button of the mouse over the target.
	/// </summary>
	private static void Click(Window window)
	{
		window.MouseDown(Inside, MouseButton.Left);

		window.MouseUp(Inside, MouseButton.Left);
	}

	/// <summary>
	/// Shows the content in a window of a fixed size and lets the layout settle.
	/// </summary>
	private static Window Show(Control content)
	{
		Window window = new()
		{
			Content = content,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
