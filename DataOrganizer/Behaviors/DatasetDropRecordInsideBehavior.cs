using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using CommunityToolkit.Mvvm.Input;
using DataOrganizer.DTO.Dataset;
using DataOrganizer.Enums;
using DataOrganizer.Helpers;
using System.Collections.ObjectModel;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Accepts a record dragged by <see cref="DatasetDragRecordBehavior" /> and moves it into the
/// associated element's position, drawing an insertion indicator while the pointer is over the target.
/// </summary>
internal sealed class DatasetDropRecordInsideBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Command invoked after a successful move.
	/// </summary>
	public IRelayCommand? MovedCommand
	{
		get => GetValue(MovedCommandProperty);
		set => SetValue(MovedCommandProperty, value);
	}

	/// <summary>
	/// Root record collection used to resolve the dragged record's owner.
	/// </summary>
	public ObservableCollection<DatasetRecordBase>? Records
	{
		get => GetValue(RecordsProperty);
		set => SetValue(RecordsProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="MovedCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<IRelayCommand?> MovedCommandProperty = AvaloniaProperty
		.Register<DatasetDropRecordInsideBehavior, IRelayCommand?>(name: nameof(MovedCommand));

	/// <summary>
	/// Identifies the <see cref="Records" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ObservableCollection<DatasetRecordBase>?> RecordsProperty = AvaloniaProperty
		.Register<DatasetDropRecordInsideBehavior, ObservableCollection<DatasetRecordBase>?>(name: nameof(Records));
	#endregion

	#region Data
	/// <summary>
	/// Insertion indicator shown in the adorner layer while a valid drag is over the target.
	/// </summary>
	private Border? _adorner;
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="DragDrop.DragLeaveEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_DragLeave(
		object? sender,
		DragEventArgs e) => RemoveAdorner();

	/// <summary>
	/// <see cref="DragDrop.DragOverEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_DragOver(
		object? sender,
		DragEventArgs e)
	{
		// Disabled in read-only mode; internal moves are forbidden, external text drag-out is unaffected.
		if (!IsEnabled)
		{
			RemoveAdorner();

			return;
		}

		// Ignore foreign drags (e.g. text drag-out or external files); leave them to default handling.
		if (e.DataTransfer.TryGetValue(DatasetDragRecordBehavior.RecordFormat) is null)
		{
			return;
		}

		if (!TryResolve(e, out _, out _, out DropPlacement placement))
		{
			e.DragEffects = DragDropEffects.None;

			RemoveAdorner();

			e.Handled = true;

			return;
		}

		e.DragEffects = DragDropEffects.Move;

		ShowAdorner(placement);

		e.Handled = true;
	}

	/// <summary>
	/// <see cref="DragDrop.DropEvent" /> handler of <see cref="StyledElementBehavior{T}.AssociatedObject" />.
	/// </summary>
	private void AssociatedObject_Drop(
		object? sender,
		DragEventArgs e)
	{
		// Disabled in read-only mode; internal moves are forbidden, external text drag-out is unaffected.
		if (!IsEnabled)
		{
			return;
		}

		// Ignore foreign drags (e.g. text drag-out or external files); leave them to default handling.
		if (e.DataTransfer.TryGetValue(DatasetDragRecordBehavior.RecordFormat) is not { } dragged)
		{
			return;
		}

		RemoveAdorner();

		if (!TryResolve(e, out ObservableCollection<DatasetRecordBase> target, out int index, out _)
			|| Records is null
			|| DatasetRecordMoveHelper.FindOwner(Records, dragged) is not { } source)
		{
			e.DragEffects = DragDropEffects.None;

			e.Handled = true;

			return;
		}

		e.DragEffects = DragDropEffects.Move;

		e.Handled = true;

		// Dropping onto its own edge leaves the record where it is; skip the move and the save.
		int sourceIndex = source.IndexOf(dragged);

		int finalIndex = ReferenceEquals(source, target) && index > sourceIndex
			? index - 1
			: index;

		if (ReferenceEquals(source, target) && finalIndex == sourceIndex)
		{
			return;
		}

		if (DatasetRecordMoveHelper.Move(source, dragged, target, index))
		{
			MovedCommand?.Execute(dragged);
		}
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

		DragDrop.SetAllowDrop(AssociatedObject, true);

		AssociatedObject.AddHandler(DragDrop.DragOverEvent, AssociatedObject_DragOver);

		AssociatedObject.AddHandler(DragDrop.DragLeaveEvent, AssociatedObject_DragLeave);

		AssociatedObject.AddHandler(DragDrop.DropEvent, AssociatedObject_Drop);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree()
	{
		if (AssociatedObject is null)
		{
			return;
		}

		RemoveAdorner();

		AssociatedObject.RemoveHandler(DragDrop.DragOverEvent, AssociatedObject_DragOver);

		AssociatedObject.RemoveHandler(DragDrop.DragLeaveEvent, AssociatedObject_DragLeave);

		AssociatedObject.RemoveHandler(DragDrop.DropEvent, AssociatedObject_Drop);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Removes the insertion indicator from the adorner layer.
	/// </summary>
	private void RemoveAdorner()
	{
		if (_adorner is null || AssociatedObject is null)
		{
			return;
		}

		AdornerLayer.GetAdornerLayer(AssociatedObject)?.Children.Remove(_adorner);

		_adorner = null;
	}

	/// <summary>
	/// Resolves the brush used to paint the insertion indicator.
	/// </summary>
	private IBrush ResolveIndicatorBrush()
	{
		return AssociatedObject?.TryFindResource("MaterialPrimaryMidBrush", out object? resource) == true
			&& resource is IBrush brush
			? brush
			: Brushes.DodgerBlue;
	}

	/// <summary>
	/// Shows the insertion indicator for <paramref name="placement" /> over the associated element.
	/// </summary>
	private void ShowAdorner(DropPlacement placement)
	{
		if (AssociatedObject is null
			|| AdornerLayer.GetAdornerLayer(AssociatedObject) is not { } layer)
		{
			return;
		}

		if (_adorner is null)
		{
			_adorner = new Border
			{
				BorderBrush = ResolveIndicatorBrush(),
				IsHitTestVisible = false
			};

			AdornerLayer.SetAdornedElement(_adorner, AssociatedObject);

			layer.Children.Add(_adorner);
		}

		_adorner.BorderThickness = placement switch
		{
			DropPlacement.Into => new Thickness(2.0),
			DropPlacement.After => new Thickness(0.0, 0.0, 0.0, 2.0),
			_ => new Thickness(0.0, 2.0, 0.0, 0.0)
		};

		_adorner.CornerRadius = placement == DropPlacement.Into
			? new CornerRadius(4.0)
			: default;
	}

	/// <summary>
	/// Resolves the drop target collection, insertion index and placement for the current pointer state.
	/// Returns <c>false</c> when the drag is foreign or the target is not allowed.
	/// </summary>
	private bool TryResolve(
		DragEventArgs e,
		out ObservableCollection<DatasetRecordBase> target,
		out int index,
		out DropPlacement placement)
	{
		target = null!;

		index = 0;

		placement = DropPlacement.Into;

		if (Records is null
			|| AssociatedObject is null
			|| e.DataTransfer.TryGetValue(DatasetDragRecordBehavior.RecordFormat) is not { } dragged)
		{
			return false;
		}

		switch (AssociatedObject.DataContext)
		{
			// Dropping onto a group places the record inside it, at the end.
			case RecordsGroup group:
				if (dragged is RecordsGroup draggedGroup
					&& DatasetRecordMoveHelper.IsSelfOrDescendant(draggedGroup, group.Children))
				{
					return false;
				}

				target = group.Children;

				index = group.Children.Count;

				return true;

			// Dropping onto a record inserts before or after it, depending on the pointer half.
			case DatasetRecordBase record:
				if (ReferenceEquals(record, dragged)
					|| DatasetRecordMoveHelper.FindOwner(Records, record) is not { } owner
					|| (dragged is RecordsGroup group2
						&& DatasetRecordMoveHelper.IsSelfOrDescendant(group2, owner)))
				{
					return false;
				}

				placement = e.GetPosition(AssociatedObject).Y > AssociatedObject.Bounds.Height / 2.0
					? DropPlacement.After
					: DropPlacement.Before;

				target = owner;

				index = owner.IndexOf(record) + (placement == DropPlacement.After ? 1 : 0);

				return true;

			// Dropping onto empty surface appends to the root collection.
			default:
				placement = DropPlacement.After;

				target = Records;

				index = Records.Count;

				return true;
		}
	}
	#endregion
}
