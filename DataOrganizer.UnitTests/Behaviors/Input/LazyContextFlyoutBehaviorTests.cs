using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AwesomeAssertions;
using DataOrganizer.Behaviors.Input;

namespace DataOrganizer.UnitTests.Behaviors.Input;

[TestFixture(Description = $@"Tests of ""{nameof(LazyContextFlyoutBehavior)}"" type")]
internal class LazyContextFlyoutBehaviorTests
{
	#region Data
	/// <summary>
	/// A point over the target of the behavior.
	/// </summary>
	private static readonly Point Inside = new(25.0, 25.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="LazyContextFlyoutBehavior.ShowOnDoubleTap" />: without it a double click builds no flyout.
	/// </summary>
	[AvaloniaTest]
	public void DoubleClick_Opens_No_Flyout_Without_ShowOnDoubleTap()
	{
		// Arrange
		Border target = new()
		{
			Background = Brushes.Transparent,
			DataContext = "Item",
			Height = 50.0,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Width = 50.0
		};

		Window window = new()
		{
			Content = target,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		LazyContextFlyoutBehavior sut = new()
		{
			ContentTemplate = new FuncDataTemplate<string>(static (_, _) => new TextBlock())
		};

		sut.Attach(target);

		// Act
		Click(window);

		Click(window);

		FlyoutBase? flyout = target.ContextFlyout;

		window.Close();

		// Assert
		flyout
			.Should()
			.BeNull();
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
	#endregion
}
