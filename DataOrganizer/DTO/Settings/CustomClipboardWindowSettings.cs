namespace DataOrganizer.DTO.Settings;

/// <summary>
/// Persisted settings of <c>CustomClipboardWindow</c>.
/// </summary>
public sealed class CustomClipboardWindowSettings : PositionSizeSettings
{
	#region Properties
	/// <summary>
	/// Whether the window stays open on focus loss and after a restore.
	/// </summary>
	public bool KeepOpen { get; init; }
	#endregion
}
