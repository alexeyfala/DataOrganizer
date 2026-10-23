using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AwesomeAssertions;
using DataOrganizer.Behaviors.DragAndDrop;

namespace DataOrganizer.UnitTests.Behaviors.DragAndDrop;

[TestFixture(Description = $@"Tests of ""{nameof(ItemDragBehavior)}"" type")]
internal class ItemDragBehaviorTests
{
	#region Data
	/// <summary>
	/// Height of a row of the list.
	/// </summary>
	private const double RowHeight = 40.0;

	/// <summary>
	/// A move down, far enough to start a drag at the default threshold.
	/// </summary>
	private static readonly Vector Far = new(0.0, 20.0);

	/// <summary>
	/// A point over the first row where the press happens.
	/// </summary>
	private static readonly Point Start = new(10.0, 10.0);
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ItemDragBehavior" />: a press whose capture is lost, as to another window or a popup, ends the gesture, so
	/// a later move with the button held drags nothing.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Capture_Lost_Ends_The_Gesture()
	{
		// Arrange
		ItemsControl list = CreateList(out Border row);

		Window window = Show(list);

		IPointer? pointer = null;

		window.AddHandler(
			InputElement.PointerPressedEvent,
			(_, e) => pointer = e.Pointer,
			RoutingStrategies.Tunnel);

		window.MouseDown(Start, MouseButton.Left);

		pointer!.Capture(null);

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		// Assert
		row.RenderTransform!.Value.M32
			.Should()
			.Be(0.0);
	}

	/// <summary>
	/// <see cref="ItemDragBehavior" />: a move with the left button held past the threshold carries the pressed row along.
	/// </summary>
	[AvaloniaTest]
	public void Pointer_Move_With_The_Left_Button_Carries_The_Row()
	{
		// Arrange
		ItemsControl list = CreateList(out Border row);

		Window window = Show(list);

		window.MouseDown(Start, MouseButton.Left);

		// Act
		window.MouseMove(Start + Far, RawInputModifiers.LeftMouseButton);

		// Assert
		row.RenderTransform!.Value.M32
			.Should()
			.Be(Far.Y);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a list of three rows, the first of them with the behavior, and returns it with that row.
	/// </summary>
	private static ItemsControl CreateList(out Border first)
	{
		first = CreateRow();

		Interaction
			.GetBehaviors(first)
			.Add(new ItemDragBehavior
			{
				Orientation = Orientation.Vertical
			});

		return new()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			Items =
			{
				first,
				CreateRow(),
				CreateRow()
			},
			VerticalAlignment = VerticalAlignment.Top,
			Width = 100.0
		};
	}

	/// <summary>
	/// Creates a row that the pointer hits.
	/// </summary>
	private static Border CreateRow()
	{
		return new()
		{
			Background = Brushes.Transparent,
			Height = RowHeight
		};
	}

	/// <summary>
	/// Shows an element in a window and lays it out.
	/// </summary>
	private static Window Show(Control element)
	{
		Window window = new()
		{
			Content = element,
			Height = 200.0,
			Width = 200.0
		};

		window.Show();

		Dispatcher.UIThread.RunJobs();

		return window;
	}
	#endregion
}
