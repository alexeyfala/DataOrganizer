using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using System;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Scrolls the associated <see cref="ScrollViewer" /> vertically while a record drag hovers near its
/// top or bottom edge, so distant drop targets can be reached during the drag.
/// </summary>
internal sealed class DatasetAutoScrollOnDragBehavior : StyledElementBehavior<ScrollViewer>
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
	/// Amount scrolled when triggered.
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

	#region Event Handlers
	/// <summary>
	/// <see cref="DragDrop.DragOverEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_DragOver(
		object? sender,
		DragEventArgs e)
	{
		if (!IsEnabled
			|| AssociatedObject is null
			|| e.DataTransfer.TryGetValue(DatasetDragRecordInsideBehavior.RecordFormat) is null)
		{
			return;
		}

		double y = e.GetPosition(AssociatedObject).Y;

		double offsetY = AssociatedObject.Offset.Y;

		double newY = offsetY;

		if (y < EdgeDistance)
		{
			newY = Math.Max(offsetY - ScrollDelta, 0.0);
		}
		else if (y > AssociatedObject.Bounds.Height - EdgeDistance)
		{
			double maxY = Math.Max(AssociatedObject.Extent.Height - AssociatedObject.Bounds.Height, 0.0);

			newY = Math.Min(offsetY + ScrollDelta, maxY);
		}

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
		// handledEventsToo: record rows mark the drag-over as handled; the edge scroll still needs it.
		AssociatedObject?.AddHandler(
			DragDrop.DragOverEvent,
			AssociatedObject_DragOver,
			handledEventsToo: true);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		AssociatedObject?.RemoveHandler(DragDrop.DragOverEvent, AssociatedObject_DragOver);
	}
	#endregion
}
