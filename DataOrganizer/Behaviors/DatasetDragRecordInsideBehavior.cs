using Avalonia;
using Avalonia.Input;
using DataOrganizer.DTO.Dataset;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Starts an in-process drag of the associated <see cref="Record" /> when the pointer is moved
/// with the <see cref="KeyModifiers.Shift" /> modifier held, so records can be reordered by dropping.
/// </summary>
internal sealed class DatasetDragRecordInsideBehavior : DatasetDragBehaviorBase
{
	#region Properties
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
	/// Identifies the <see cref="Record" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DatasetRecordBase?> RecordProperty = AvaloniaProperty
		.Register<DatasetDragRecordInsideBehavior, DatasetRecordBase?>(name: nameof(Record));
	#endregion

	#region Data
	/// <summary>
	/// Application-private format carrying the dragged record; never serialized to other processes.
	/// </summary>
	public static readonly DataFormat<DatasetRecordBase> RecordFormat = DataFormat
		.CreateInProcessFormat<DatasetRecordBase>("DataOrganizer.Dataset.RecordReorder");
	#endregion

	#region Methods
	/// <inheritdoc />
	protected override DragDropEffects AllowedEffects => DragDropEffects.Move;

	/// <inheritdoc />
	protected override bool CanStartDrag(PointerEventArgs e) =>
		e.KeyModifiers.HasFlag(KeyModifiers.Shift) && Record is not null;

	/// <inheritdoc />
	protected override DataTransfer? CreateDataTransfer()
	{
		if (Record is not { } record)
		{
			return null;
		}

		DataTransfer data = new();

		data.Add(DataTransferItem.Create(RecordFormat, record));

		return data;
	}
	#endregion
}
