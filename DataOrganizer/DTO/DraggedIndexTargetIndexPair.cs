namespace DataOrganizer.DTO;

public readonly struct DraggedIndexTargetIndexPair
{
	#region Properties
	/// <summary>
	/// Dragged index.
	/// </summary>
	public required int DraggedIndex { get; init; }

	/// <summary>
	/// Target index.
	/// </summary>
	public required int TargetIndex { get; init; }
	#endregion
}
