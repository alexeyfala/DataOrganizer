using Avalonia;
using Avalonia.Input;
using DataOrganizer.DTO.Dataset;

namespace DataOrganizer.Behaviors;

/// <summary>
/// Drags the associated record with the mouse alone, carrying both an in-process payload for reordering
/// inside the editor and plain text for other applications; the drop location decides which is consumed.
/// </summary>
internal sealed class DatasetDragRecordBehavior : DatasetDragBehaviorBase
{
	#region Properties
	/// <summary>
	/// Record carried by the drag operation for in-editor reordering.
	/// </summary>
	public DatasetRecordBase? Record
	{
		get => GetValue(RecordProperty);
		set => SetValue(RecordProperty, value);
	}

	/// <summary>
	/// Text carried by the drag operation for other applications.
	/// </summary>
	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}
	#endregion

	#region Styled Properties
	/// <summary>
	/// Identifies the <see cref="Record" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<DatasetRecordBase?> RecordProperty = AvaloniaProperty
		.Register<DatasetDragRecordBehavior, DatasetRecordBase?>(name: nameof(Record));

	/// <summary>
	/// Identifies the <see cref="Text" /> avalonia property.
	/// </summary>
	public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty
		.Register<DatasetDragRecordBehavior, string?>(name: nameof(Text));
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
	protected override DragDropEffects AllowedEffects => DragDropEffects.Copy | DragDropEffects.Move;

	/// <inheritdoc />
	protected override bool CanStartDrag(PointerEventArgs e) =>
		Record is not null || !string.IsNullOrWhiteSpace(Text);

	/// <inheritdoc />
	protected override DataTransfer? CreateDataTransfer()
	{
		DataTransfer? data = null;

		if (!string.IsNullOrWhiteSpace(Text))
		{
			data = new();

			data.Add(DataTransferItem.CreateText(Text));
		}

		if (Record is { } record)
		{
			data ??= new();

			data.Add(DataTransferItem.Create(RecordFormat, record));
		}

		return data;
	}
	#endregion
}
