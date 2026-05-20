using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

// Source code taken from:
// https://github.com/wieslawsoltes/Xaml.Behaviors/blob/master/src/Xaml.Behaviors.Interactions.Draggable/AutoScrollDuringDragBehavior.cs

/// <summary>
/// Automatically scrolls the associated <see cref="ScrollViewer"/> when the pointer is dragged near its edges.
/// </summary>
internal sealed class AutoScrollDuringDragBehavior : StyledElementBehavior<Visual>
{
	#region Properties
	/// <summary>
	/// Gets or sets the distance from the edge that triggers scrolling.
	/// </summary>
	public double EdgeDistance
	{
		get => GetValue(EdgeDistanceProperty);
		set => SetValue(EdgeDistanceProperty, value);
	}

	/// <summary>
	/// Gets or sets the amount scrolled when triggered.
	/// </summary>
	public double ScrollDelta
	{
		get => GetValue(ScrollDeltaProperty);
		set => SetValue(ScrollDeltaProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="EdgeDistance"/> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> EdgeDistanceProperty = AvaloniaProperty
		.Register<AutoScrollDuringDragBehavior, double>(nameof(EdgeDistance), 20);

	/// <summary>
	/// Identifies the <see cref="ScrollDelta"/> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> ScrollDeltaProperty = AvaloniaProperty
		.Register<AutoScrollDuringDragBehavior, double>(nameof(ScrollDelta), 10);
	#endregion

	#region Data
	/// <summary>
	/// Returns <c>True</c> if a drag is in progress.
	/// </summary>
	private bool _dragging;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerCaptureLostEvent" /> handler of <see cref="ScrollViewer" />.
	/// </summary>
	private void ScrollViewer_PointerCaptureLost(
		object? sender,
		PointerCaptureLostEventArgs e)
	{
		_dragging = false;
	}

	/// <summary>
	/// <see cref="InputElement.PointerMovedEvent" /> handler of <see cref="ScrollViewer" />.
	/// </summary>
	private void ScrollViewer_PointerMoved(
		object? sender,
		PointerEventArgs e)
	{
		if (!IsEnabled
			|| !_dragging
			|| sender is not ScrollViewer scrollViewer)
		{
			return;
		}

		Point position = e.GetPosition(scrollViewer);

		double newX = scrollViewer.Offset.X;

		double newY = scrollViewer.Offset.Y;

		if (position.X < EdgeDistance)
		{
			newX = Math.Max(newX - ScrollDelta, 0);
		}
		else if (position.X > scrollViewer.Bounds.Width - EdgeDistance)
		{
			newX = Math.Min(newX + ScrollDelta, Math.Max(scrollViewer.Extent.Width - scrollViewer.Bounds.Width, 0));
		}

		if (position.Y < EdgeDistance)
		{
			newY = Math.Max(newY - ScrollDelta, 0);
		}
		else if (position.Y > scrollViewer.Bounds.Height - EdgeDistance)
		{
			newY = Math.Min(newY + ScrollDelta, Math.Max(scrollViewer.Extent.Height - scrollViewer.Bounds.Height, 0));
		}

		if (Math.Abs(newX - scrollViewer.Offset.X) > double.Epsilon || Math.Abs(newY - scrollViewer.Offset.Y) > double.Epsilon)
		{
			scrollViewer.Offset = new Vector(newX, newY);
		}
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="ScrollViewer" />.
	/// </summary>
	private void ScrollViewer_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		if (!IsEnabled
			|| sender is not ScrollViewer scrollViewer
			|| !e.GetCurrentPoint(scrollViewer).Properties.IsLeftButtonPressed)
		{
			return;
		}

		_dragging = true;
	}

	/// <summary>
	/// <see cref="InputElement.PointerReleasedEvent" /> handler of <see cref="ScrollViewer" />.
	/// </summary>
	private void ScrollViewer_PointerReleased(
		object? sender,
		PointerReleasedEventArgs e)
	{
		if (e.InitialPressMouseButton != MouseButton.Left)
		{
			return;
		}

		_dragging = false;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		base.OnDetachedFromVisualTree();

		if (AssociatedObject?.FindDescendantOfType<ScrollViewer>() is not { } scrollViewer)
		{
			return;
		}

		scrollViewer.RemoveHandler(
			InputElement.PointerCaptureLostEvent,
			ScrollViewer_PointerCaptureLost);

		scrollViewer.RemoveHandler(
			InputElement.PointerMovedEvent,
			ScrollViewer_PointerMoved);

		scrollViewer.RemoveHandler(
			InputElement.PointerPressedEvent,
			ScrollViewer_PointerPressed);

		scrollViewer.RemoveHandler(
			InputElement.PointerReleasedEvent,
			ScrollViewer_PointerReleased);
	}

	/// <inheritdoc />
	protected override void OnLoaded()
	{
		base.OnLoaded();

		if (AssociatedObject?.FindDescendantOfType<ScrollViewer>() is not { } scrollViewer)
		{
			return;
		}

		scrollViewer.AddHandler(
			InputElement.PointerCaptureLostEvent,
			ScrollViewer_PointerCaptureLost,
			RoutingStrategies.Tunnel);

		scrollViewer.AddHandler(
			InputElement.PointerMovedEvent,
			ScrollViewer_PointerMoved,
			RoutingStrategies.Tunnel);

		scrollViewer.AddHandler(
			InputElement.PointerPressedEvent,
			ScrollViewer_PointerPressed,
			RoutingStrategies.Tunnel);

		scrollViewer.AddHandler(
			InputElement.PointerReleasedEvent,
			ScrollViewer_PointerReleased,
			RoutingStrategies.Tunnel);
	}
	#endregion
}
