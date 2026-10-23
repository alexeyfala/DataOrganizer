using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;
using DataOrganizer.UnitTests.Factories;

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
		window.Click(Inside);

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
		window.Click(Inside);

		window.Click(Inside);

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
		window.Click(Inside);

		window.Click(Inside);

		// Assert
		flyout.IsOpen
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
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
