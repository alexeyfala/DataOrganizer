namespace DataOrganizer.Dto.Settings;

/// <summary>
/// The stored position of a window.
/// </summary>
public class PositionSettings
{
	#region Properties
	/// <summary>
	/// The X coordinate of the window.
	/// </summary>
	public required int X { get; init; }

	/// <summary>
	/// The Y coordinate of the window.
	/// </summary>
	public required int Y { get; init; }
	#endregion
}
