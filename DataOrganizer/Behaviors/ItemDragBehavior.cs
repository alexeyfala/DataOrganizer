using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Transformation;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO;
using System;

namespace DataOrganizer.Behaviors;

// Source code taken from:
// https://github.com/wieslawsoltes/Xaml.Behaviors/blob/master/src/Xaml.Behaviors.Interactions.Draggable/ItemDragBehavior.cs

/// <summary>
/// Allows dragging items within an <see cref="ItemsControl"/>.
/// </summary>
internal sealed class ItemDragBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Gets or sets the horizontal drag threshold in pixels.
	/// </summary>
	public double HorizontalDragThreshold
	{
		get => GetValue(HorizontalDragThresholdProperty);
		set => SetValue(HorizontalDragThresholdProperty, value);
	}

	/// <summary>
	/// Command to detect when the item has dragged.
	/// </summary>
	public IRelayCommand? ItemDraggedCommand
	{
		get => GetValue(ItemDraggedCommandProperty);
		set => SetValue(ItemDraggedCommandProperty, value);
	}

	/// <summary>
	/// Gets or sets the orientation of the drag operation.
	/// </summary>
	public Orientation Orientation
	{
		get => GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	/// <summary>
	/// Gets or sets the vertical drag threshold in pixels.
	/// </summary>
	public double VerticalDragThreshold
	{
		get => GetValue(VerticalDragThresholdProperty);
		set => SetValue(VerticalDragThresholdProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="HorizontalDragThreshold"/> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> HorizontalDragThresholdProperty = AvaloniaProperty
		.Register<ItemDragBehavior, double>(name: nameof(HorizontalDragThreshold), 3);

	/// <summary>
	/// Identifies the <see cref="ItemDraggedCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IRelayCommand?> ItemDraggedCommandProperty = AvaloniaProperty
		.Register<ItemDragBehavior, IRelayCommand?>(name: nameof(ItemDraggedCommand));

	/// <summary>
	/// Identifies the <see cref="Orientation"/> avalonia property.
	/// </summary>
	public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty
		.Register<ItemDragBehavior, Orientation>(name: nameof(Orientation));

	/// <summary>
	/// Identifies the <see cref="VerticalDragThreshold"/> avalonia property.
	/// </summary>
	public static readonly StyledProperty<double> VerticalDragThresholdProperty = AvaloniaProperty
		.Register<ItemDragBehavior, double>(name: nameof(VerticalDragThreshold), 3);
	#endregion

	#region Data
	private bool _captured;

	private Control? _draggedContainer;

	private int _draggedIndex;

	private bool _dragStarted;

	private bool _enableDrag;

	private ItemsControl? _itemsControl;

	private Point _start;

	private int _targetIndex;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerCaptureLostEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerCaptureLost(
		object? sender,
		PointerCaptureLostEventArgs e)
	{
		Released();

		_captured = false;
	}

	/// <summary>
	/// <see cref="InputElement.PointerMovedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerMoved(
		object? sender,
		PointerEventArgs e)
	{
		PointerPointProperties properties = e
			.GetCurrentPoint(AssociatedObject)
			.Properties;

		if (!IsEnabled
			|| !_captured
			|| !properties.IsLeftButtonPressed
			|| _itemsControl?.Items is not { } items
			|| _draggedContainer?.RenderTransform is null
			|| !_enableDrag)
		{
			return;
		}

		Point position = e.GetPosition(_itemsControl);

		double delta = Orientation == Orientation.Horizontal
			? position.X - _start.X
			: position.Y - _start.Y;

		if (!_dragStarted)
		{
			Point diff = _start - position;

			if (Orientation == Orientation.Horizontal)
			{
				if (Math.Abs(diff.X) > HorizontalDragThreshold)
				{
					_dragStarted = true;
				}
				else
				{
					return;
				}
			}
			else
			{
				if (Math.Abs(diff.Y) > VerticalDragThreshold)
				{
					_dragStarted = true;
				}
				else
				{
					return;
				}
			}
		}

		if (Orientation == Orientation.Horizontal)
		{
			SetTranslateTransform(_draggedContainer, delta, 0.0);
		}
		else
		{
			SetTranslateTransform(_draggedContainer, 0.0, delta);
		}

		_draggedIndex = _itemsControl.IndexFromContainer(_draggedContainer);

		_targetIndex = -1;

		double draggedStart = Orientation == Orientation.Horizontal
			? _draggedContainer.Bounds.X
			: _draggedContainer.Bounds.Y;

		double draggedDeltaStart = Orientation == Orientation.Horizontal
			? _draggedContainer.Bounds.X + delta
			: _draggedContainer.Bounds.Y + delta;

		double draggedDeltaEnd = Orientation == Orientation.Horizontal
			? _draggedContainer.Bounds.X + delta + _draggedContainer.Bounds.Width
			: _draggedContainer.Bounds.Y + delta + _draggedContainer.Bounds.Height;

		for (int i = 0; i < items.Count; i++)
		{
			Control? targetContainer = _itemsControl.ContainerFromIndex(i);

			if (targetContainer?.RenderTransform is null || ReferenceEquals(targetContainer, _draggedContainer))
			{
				continue;
			}

			Rect targetBounds = targetContainer.Bounds;

			double targetStart = Orientation == Orientation.Horizontal
				? targetBounds.X
				: targetBounds.Y;

			double targetMid = Orientation == Orientation.Horizontal
				? targetBounds.X + targetBounds.Width / 2.0
				: targetBounds.Y + targetBounds.Height / 2.0;

			int targetIndex = _itemsControl.IndexFromContainer(targetContainer);

			if (targetStart > draggedStart && draggedDeltaEnd >= targetMid)
			{
				if (Orientation == Orientation.Horizontal)
				{
					SetTranslateTransform(
						targetContainer,
						-_draggedContainer.Bounds.Width,
						0.0);
				}
				else
				{
					SetTranslateTransform(
						targetContainer,
						0.0,
						-_draggedContainer.Bounds.Height);
				}

				_targetIndex = _targetIndex == -1
					? targetIndex
					: targetIndex > _targetIndex
						? targetIndex
						: _targetIndex;
			}
			else if (targetStart < draggedStart && draggedDeltaStart <= targetMid)
			{
				if (Orientation == Orientation.Horizontal)
				{
					SetTranslateTransform(
						targetContainer,
						_draggedContainer.Bounds.Width,
						0.0);
				}
				else
				{
					SetTranslateTransform(
						targetContainer,
						0.0,
						_draggedContainer.Bounds.Height);
				}

				_targetIndex = _targetIndex == -1 ? targetIndex
					: targetIndex < _targetIndex
						? targetIndex
						: _targetIndex;
			}
			else
			{
				if (Orientation == Orientation.Horizontal)
				{
					SetTranslateTransform(
						targetContainer,
						0.0,
						0.0);
				}
				else
				{
					SetTranslateTransform(
						targetContainer,
						0.0,
						0.0);
				}
			}
		}
	}

	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerPressed(
		object? sender,
		PointerPressedEventArgs e)
	{
		PointerPointProperties properties = e
			.GetCurrentPoint(AssociatedObject)
			.Properties;

		if (!IsEnabled
			|| !properties.IsLeftButtonPressed
			|| AssociatedObject?.Parent is not ItemsControl itemsControl)
		{
			return;
		}

		_enableDrag = true;

		_dragStarted = false;

		_start = e.GetPosition(itemsControl);

		_draggedIndex = -1;

		_targetIndex = -1;

		_itemsControl = itemsControl;

		_draggedContainer = AssociatedObject;

		if (AssociatedObject is not null)
		{
			SetDraggingPseudoClasses(AssociatedObject, true);
		}

		ApplyTransforms(_itemsControl);

		_captured = true;
	}

	/// <summary>
	/// <see cref="InputElement.PointerReleasedEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_PointerReleased(
		object? sender,
		PointerReleasedEventArgs e)
	{
		if (!_captured)
		{
			return;
		}

		if (e.InitialPressMouseButton == MouseButton.Left)
		{
			Released();
		}

		_captured = false;
	}
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
	private static void ApplyTransforms(ItemsControl? itemsControl)
	{
		if (itemsControl?.Items is not { } items)
		{
			return;
		}

		for (int i = 0; i < items.Count; i++)
		{
			if (itemsControl.ContainerFromIndex(i) is { } control)
			{
				SetTranslateTransform(
					control,
					0.0,
					0.0);
			}
		}
	}

	private static void SetDraggingPseudoClasses(Control control, bool isDragging)
	{
		if (isDragging)
		{
			((IPseudoClasses)control.Classes).Add(":dragging");
		}
		else
		{
			((IPseudoClasses)control.Classes).Remove(":dragging");
		}
	}

	private static void SetTranslateTransform(
		Control control,
		double x,
		double y)
	{
		TransformOperations.Builder builder = new(1);

		builder.AppendTranslate(x, y);

		control.RenderTransform = builder.Build();
	}

	private void Released()
	{
		if (!_enableDrag)
		{
			return;
		}

		ApplyTransforms(_itemsControl);

		if (_itemsControl is not null)
		{
			foreach (Control control in _itemsControl.GetRealizedContainers())
			{
				SetDraggingPseudoClasses(control, true);
			}
		}

		if (_dragStarted)
		{
			if (_draggedIndex >= 0 && _targetIndex >= 0 && _draggedIndex != _targetIndex)
			{
				ItemDraggedCommand?.Execute(new DraggedIndexTargetIndexPair
				{
					DraggedIndex = _draggedIndex,
					TargetIndex = _targetIndex
				});
			}
		}

		if (_itemsControl is not null)
		{
			foreach (Control control in _itemsControl.GetRealizedContainers())
			{
				SetDraggingPseudoClasses(control, false);
			}
		}

		if (_draggedContainer is not null)
		{
			SetDraggingPseudoClasses(_draggedContainer, false);
		}

		_draggedIndex = -1;

		_targetIndex = -1;

		_enableDrag = false;

		_dragStarted = false;

		_itemsControl = null;

		_draggedContainer = null;
	}
	#endregion
}
