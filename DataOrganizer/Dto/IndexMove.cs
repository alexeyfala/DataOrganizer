namespace DataOrganizer.Dto;

/// <summary>
/// The move of an item from one position in a collection to another.
/// </summary>
public readonly struct IndexMove
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
