using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.DTO.Dataset;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Starts an in-process drag of the associated <see cref="Record" /> when the pointer is moved
/// with the <see cref="KeyModifiers.Shift" /> modifier held, so records can be reordered by dropping.
/// </summary>
internal sealed class DatasetRecordReorderDragBehavior : Behavior<Control>
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
	/// <c>True</c> forbids starting a drag.
	/// </summary>
	public bool IsReadOnly
	{
		get => GetValue(IsReadOnlyProperty);
		set => SetValue(IsReadOnlyProperty, value);
	}

	/// <summary>
	/// Record carried by the drag operation.
	/// </summary>
	public DatasetRecordBase? Record
	{
		get => GetValue(RecordProperty);
		set => SetValue(RecordProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="DragThreshold" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> DragThresholdProperty = AvaloniaProperty
		.Register<DatasetRecordReorderDragBehavior, double>(name: nameof(DragThreshold), 4.0);

	/// <summary>
	/// Identifies the <see cref="IsReadOnly" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty
		.Register<DatasetRecordReorderDragBehavior, bool>(name: nameof(IsReadOnly));

	/// <summary>
	/// Identifies the <see cref="Record" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DatasetRecordBase?> RecordProperty = AvaloniaProperty
		.Register<DatasetRecordReorderDragBehavior, DatasetRecordBase?>(name: nameof(Record));
	#endregion

	#region Data
	/// <summary>
	/// Application-private format carrying the dragged record; never serialized to other processes.
	/// </summary>
	public static readonly DataFormat<DatasetRecordBase> RecordFormat = DataFormat
		.CreateInProcessFormat<DatasetRecordBase>("DataOrganizer.Dataset.RecordReorder");

	/// <summary>
	/// <c>True</c> once the drag operation has been started for the current press.
	/// </summary>
	private bool _dragStarted;

	/// <summary>
	/// <c>True</c> while the left button is held after a press on the associated element.
	/// </summary>
	private bool _pressed;

	/// <summary>
	/// Pointer position at press time, used to measure the drag threshold.
	/// </summary>
	private Point _start;

	/// <summary>
	/// Press event that seeds the drag operation.
	/// </summary>
	private PointerPressedEventArgs? _triggerEvent;
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
		if (IsReadOnly
			|| !_pressed
			|| _dragStarted
			|| AssociatedObject is null
			|| Record is null
			|| !e.KeyModifiers.HasFlag(KeyModifiers.Shift)
			|| !e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
		{
			return;
		}

		Point diff = _start - e.GetPosition(AssociatedObject);

		if (Math.Abs(diff.X) < DragThreshold && Math.Abs(diff.Y) < DragThreshold)
		{
			return;
		}

		_dragStarted = true;

		_ = StartDragAsync();
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		if (IsReadOnly
			|| AssociatedObject is null
			|| !e.GetCurrentPoint(AssociatedObject).Properties.IsLeftButtonPressed)
		{
			return;
		}

		_pressed = true;

		_dragStarted = false;

		_start = e.GetPosition(AssociatedObject);

		_triggerEvent = e;
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

		// handledEventsToo: an ancestor (e.g. the group Expander header) may mark the tunneled
		// press as handled before it reaches this element; the gesture must still observe it.
		AssociatedObject.AddHandler(
			InputElement.PointerPressedEvent,
			AssociatedObject_PointerPressed,
			RoutingStrategies.Tunnel,
			handledEventsToo: true);

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

		_triggerEvent = null;
	}

	/// <summary>
	/// Runs a move drag-and-drop operation carrying <see cref="Record" /> in-process.
	/// </summary>
	private async Task StartDragAsync()
	{
		if (_triggerEvent is not { } triggerEvent || Record is not { } record)
		{
			return;
		}

		using DataTransfer data = new();

		data.Add(DataTransferItem.Create(RecordFormat, record));

		try
		{
			await DragDrop
				.DoDragDropAsync(triggerEvent, data, DragDropEffects.Move)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			// A failed drag must not crash the app; the gesture is simply abandoned.
			Trace.WriteLine(ex);
		}
		finally
		{
			Reset();
		}
	}
	#endregion
}
