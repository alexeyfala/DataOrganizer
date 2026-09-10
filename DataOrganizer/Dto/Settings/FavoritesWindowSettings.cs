namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>FavoritesWindow</c>.
/// </summary>
public sealed class FavoritesWindowSettings : PositionSettings
{
	#region Properties
	/// <summary>
	/// Popup panel height.
	/// </summary>
	public required double PopupHeight { get; init; }

	/// <summary>
	/// Popup panel width.
	/// </summary>
	public required double PopupWidth { get; init; }
	#endregion
}
