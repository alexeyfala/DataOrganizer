namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Scroll position of a text: how far its view is moved from the top left corner, in device-independent pixels.
/// </summary>
public readonly record struct ScrollOffset
{
	#region Properties
	/// <summary>
	/// Horizontal offset.
	/// </summary>
	public required double X { get; init; }

	/// <summary>
	/// Vertical offset.
	/// </summary>
	public required double Y { get; init; }
	#endregion
}
