namespace DataOrganizer.DTO;

internal readonly struct DatasetProperties
{
	#region Properties
	/// <summary>
	/// The offset of vertical scrolling position.
	/// </summary>
	public required double VerticalScrollOffset { get; init; }
	#endregion
}
