namespace DataOrganizer.DTO;

internal readonly struct DatasetProperties
{
	#region Properties
	/// <summary>
	/// Index of the top-most visible record in the dataset list.
	/// Negative value (default <c>-1</c>) means the position has not been saved yet.
	/// </summary>
	public required int TopRecordIndex { get; init; }

	/// <summary>
	/// Pixel offset within the record at <see cref="TopRecordIndex" /> — the
	/// distance from that record's top edge to the viewport's top edge at the
	/// moment of saving. Lets the restore step land on the same scroll position
	/// even if the record has variable height.
	/// </summary>
	public required double WithinRecordOffset { get; init; }
	#endregion
}
