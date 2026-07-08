using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Enables dragging the associated element's <see cref="Text" /> out to other applications
/// as plain text, bypassing the system clipboard.
/// </summary>
internal sealed class DragTextOutBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Pointer travel in pixels required before a drag starts.
	/// </summary>
	public double DragThreshold
	{
		get => GetValue(DragThresholdProperty);
		set => SetValue(DragThresholdProperty, value);
	}

	/// <summary>
	/// <c>True</c> disables dragging.
	/// </summary>
	public bool IsDisabled
	{
		get => GetValue(IsDisabledProperty);
		set => SetValue(IsDisabledProperty, value);
	}

	/// <summary>
	/// Text carried by the drag operation.
	/// </summary>
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="DragThreshold" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> DragThresholdProperty = AvaloniaProperty
		.Register<DragTextOutBehavior, double>(name: nameof(DragThreshold), 4.0);

	/// <summary>
	/// Identifies the <see cref="IsDisabled" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsDisabledProperty = AvaloniaProperty
		.Register<DragTextOutBehavior, bool>(name: nameof(IsDisabled));

	/// <summary>
	/// Identifies the <see cref="Text" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty
		.Register<DragTextOutBehavior, string?>(name: nameof(Text));
	#endregion

	#region Data
	private bool _dragStarted;

	private bool _pressed;

	private Point _start;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerCaptureLostEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerCaptureLost(
		object? sender,
		PointerCaptureLostEventArgs e) => Reset();

	/// <summary>
	/// <see cref="InputElement.PointerMovedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerMoved(
		object? sender,
		PointerEventArgs e)
	{
		if (IsDisabled
			|| !_pressed
			|| _dragStarted
			|| AssociatedObject is null
			|| !e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed
			|| string.IsNullOrWhiteSpace(Text))
		{
			return;
		}

		Point diff = _start - e.GetPosition(AssociatedObject);

		if (Math.Abs(diff.X) < DragThreshold && Math.Abs(diff.Y) < DragThreshold)
		{
			return;
		}

		_dragStarted = true;

		// Stage 2: build a DataTransfer with DataFormat.Text and call DragDrop.DoDragDropAsync here.
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		if (IsDisabled
			|| AssociatedObject is null
			|| !e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
		{
			return;
		}

		_pressed = true;

		_dragStarted = false;

		_start = e.GetPosition(AssociatedObject);
	}

	/// <summary>
	/// <see cref="InputElement.PointerReleasedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerReleased(
		object? sender,
		PointerReleasedEventArgs e) => Reset();
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttachedToVisualTree()
	{
		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.AddHandler(
			InputElement.PointerCaptureLostEvent,
			AssociatedObject_PointerCaptureLost,
			RoutingStrategies.Tunnel);

		AssociatedObject.AddHandler(
			InputElement.PointerMovedEvent,
			AssociatedObject_PointerMoved,
			RoutingStrategies.Tunnel);

		AssociatedObject.AddHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed,
			RoutingStrategies.Tunnel);

		AssociatedObject.AddHandler(
			InputElement.PointerReleasedEvent,
			AssociatedObject_PointerReleased,
			RoutingStrategies.Tunnel);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		if (AssociatedObject is null)
		{
			return;
		}

		AssociatedObject.RemoveHandler(
			InputElement.PointerCaptureLostEvent,
			AssociatedObject_PointerCaptureLost);

		AssociatedObject.RemoveHandler(
			InputElement.PointerMovedEvent,
			AssociatedObject_PointerMoved);

		AssociatedObject.RemoveHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed);

		AssociatedObject.RemoveHandler(
			InputElement.PointerReleasedEvent,
			AssociatedObject_PointerReleased);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Clears the pending drag-gesture state.
	/// </summary>
	private void Reset()
	{
		_pressed = false;

		_dragStarted = false;
	}
	#endregion
}
