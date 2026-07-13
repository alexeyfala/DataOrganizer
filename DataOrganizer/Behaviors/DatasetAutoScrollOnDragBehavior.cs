using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Messages;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Scrolls the associated <see cref="ScrollViewer" /> vertically while a record drag hovers near its
/// top or bottom edge, so distant drop targets can be reached during the drag.
/// </summary>
internal sealed class DatasetAutoScrollOnDragBehavior
	: StyledElementBehavior<ScrollViewer>, IRecipient<DatasetRecordDragEndedMessage>
{
	#region Properties
	/// <summary>
	/// Distance from the edge that triggers scrolling.
	/// </summary>
	public double EdgeDistance
	{
		get => GetValue(EdgeDistanceProperty);
		set => SetValue(EdgeDistanceProperty, value);
	}

	/// <summary>
	/// Maximum pixels scrolled per timer tick, reached at full edge proximity.
	/// </summary>
	public double ScrollDelta
	{
		get => GetValue(ScrollDeltaProperty);
		set => SetValue(ScrollDeltaProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="EdgeDistance" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> EdgeDistanceProperty = AvaloniaProperty
		.Register<DatasetAutoScrollOnDragBehavior, double>(name: nameof(EdgeDistance), 24.0);

	/// <summary>
	/// Identifies the <see cref="ScrollDelta" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> ScrollDeltaProperty = AvaloniaProperty
		.Register<DatasetAutoScrollOnDragBehavior, double>(name: nameof(ScrollDelta), 24.0);
	#endregion

	#region Data
	/// <summary>
	/// Timer interval driving the continuous edge scroll.
	/// </summary>
	private static readonly TimeSpan ScrollInterval = TimeSpan.FromMilliseconds(30.0);

	/// <summary>
	/// Current scroll direction: <c>-1</c> up, <c>1</c> down, <c>0</c> idle.
	/// </summary>
	private int _direction;

	/// <summary>
	/// Edge-proximity factor in the range 0..1 scaling the per-tick step.
	/// </summary>
	private double _intensity;

	/// <summary>
	/// Timer scrolling continuously while the pointer rests near an edge.
	/// </summary>
	private DispatcherTimer? _timer;
	#endregion

	#region Event Handlers
	/// <summary>
	/// Stops the scroll when a record drag ends.
	/// </summary>
	public void Receive(DatasetRecordDragEndedMessage message)
	{
		_direction = 0;

		_timer?.Stop();
	}

	/// <summary>
	/// <see cref="DragDrop.DragOverEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_DragOver(
		object? sender,
		DragEventArgs e)
	{
		if (!IsEnabled
			|| AssociatedObject is null
			|| e.DataTransfer.TryGetValue(DatasetDragRecordBehavior.RecordFormat) is null)
		{
			_direction = 0;

			return;
		}

		double y = e.GetPosition(AssociatedObject).Y;

		double height = AssociatedObject.Bounds.Height;

		double maxY = Math.Max(AssociatedObject.Extent.Height - height, 0.0);

		// Arm an upward/downward direction only while there is room to scroll that way.
		if (y < EdgeDistance && AssociatedObject.Offset.Y > 0.0)
		{
			_direction = -1;

			_intensity = Math.Clamp((EdgeDistance - y) / EdgeDistance, 0.0, 1.0);
		}
		else if (y > height - EdgeDistance && AssociatedObject.Offset.Y < maxY)
		{
			_direction = 1;

			_intensity = Math.Clamp((y - (height - EdgeDistance)) / EdgeDistance, 0.0, 1.0);
		}
		else
		{
			// Outside the edge zone: let the drop targets decide the cursor and stop scrolling.
			_direction = 0;

			return;
		}

		// Keep the cursor in the allowed state over edge gaps that host no drop target.
		e.DragEffects = DragDropEffects.Move;

		e.Handled = true;

		_timer?.Start();
	}

	/// <summary>
	/// <see cref="DispatcherTimer.Tick" /> handler scrolling by one proximity-scaled step.
	/// </summary>
	private void Timer_Tick(
		object? sender,
		EventArgs e)
	{
		if (AssociatedObject is null || _direction == 0)
		{
			return;
		}

		double offsetY = AssociatedObject.Offset.Y;

		double maxY = Math.Max(AssociatedObject.Extent.Height - AssociatedObject.Bounds.Height, 0.0);

		// Floor the factor so the very inner edge of the zone still creeps rather than stalling.
		double step = ScrollDelta * Math.Max(_intensity, 0.15) * _direction;

		double newY = Math.Clamp(offsetY + step, 0.0, maxY);

		if (Math.Abs(newY - offsetY) > double.Epsilon)
		{
			AssociatedObject.Offset = new Vector(AssociatedObject.Offset.X, newY);
		}
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override void OnAttachedToVisualTree()
	{
		_timer = new DispatcherTimer { Interval = ScrollInterval };

		_timer.Tick += Timer_Tick;

		// handledEventsToo: record rows mark the drag-over as handled; the edge scroll still needs it.
		AssociatedObject?.AddHandler(
			DragDrop.DragOverEvent,
			AssociatedObject_DragOver,
			handledEventsToo: true);

		WeakReferenceMessenger
			.Default
			.RegisterAll(this);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		WeakReferenceMessenger
			.Default
			.UnregisterAll(this);

		if (_timer is not null)
		{
			_timer.Stop();

			_timer.Tick -= Timer_Tick;

			_timer = null;
		}

		AssociatedObject?.RemoveHandler(DragDrop.DragOverEvent, AssociatedObject_DragOver);
	}
	#endregion
}
