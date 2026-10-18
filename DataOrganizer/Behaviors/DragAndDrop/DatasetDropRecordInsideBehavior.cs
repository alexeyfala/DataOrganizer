using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using DataOrganizer.Dto.Dataset;
using DataOrganizer.Enums.Dataset;
using DataOrganizer.Helpers.Dataset;
using DataOrganizer.Models.Dataset;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DataOrganizer.Behaviors.DragAndDrop;

/// <summary>
/// Accepts a record dragged by <see cref="DatasetRecordDragBehavior" /> onto the associated element and hands its move to
/// <see cref="MoveCommand" />, drawing an insertion indicator while the pointer is over the target.
/// </summary>
internal sealed class DatasetDropRecordInsideBehavior : Behavior<Control>
{
	#region Properties
	/// <summary>
	/// Command that moves the dropped record, given a <see cref="DatasetRecordMove" />.
	/// </summary>
	public ICommand? MoveCommand
	{
		get => GetValue(MoveCommandProperty);
		set => SetValue(MoveCommandProperty, value);
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
	/// Identifies the <see cref="MoveCommand" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<ICommand?> MoveCommandProperty = AvaloniaProperty
		.Register<DatasetDropRecordInsideBehavior, ICommand?>(name: nameof(MoveCommand));

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
		if (e.DataTransfer.TryGetValue(DatasetRecordDragBehavior.RecordFormat) is null)
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
		if (e.DataTransfer.TryGetValue(DatasetRecordDragBehavior.RecordFormat) is not { } dragged)
		{
			return;
		}

		RemoveAdorner();

		if (!TryResolve(e, out ObservableCollection<DatasetRecordBase> target, out int index, out _)
			|| Records is null
			|| DatasetRecordTree.FindOwner(Records, dragged) is null)
		{
			e.DragEffects = DragDropEffects.None;

			e.Handled = true;

			return;
		}

		e.DragEffects = DragDropEffects.Move;

		e.Handled = true;

		MoveCommand?.Execute(new DatasetRecordMove
		{
			Index = index,
			Record = dragged,
			Target = target
		});
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
			|| e.DataTransfer.TryGetValue(DatasetRecordDragBehavior.RecordFormat) is not { } dragged)
		{
			return false;
		}

		double pointerYRatio = AssociatedObject.Bounds.Height > 0.0
			? e.GetPosition(AssociatedObject).Y / AssociatedObject.Bounds.Height
			: 0.0;

		return DatasetRecordTree.TryResolveTarget(
			Records,
			dragged,
			AssociatedObject.DataContext as DatasetRecordBase,
			pointerYRatio,
			out target,
			out index,
			out placement);
	}
	#endregion
}
