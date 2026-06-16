using DataOrganizer.Enums.Clipboard;

namespace DataOrganizer.DTO.Settings;

/// <summary>
/// Persisted settings of <c>ClipboardLogWindow</c>.
/// </summary>
public sealed class ClipboardLogWindowSettings : PositionSizeSettings
{
	#region Properties
	/// <summary>
	/// Last selected type filter.
	/// </summary>
	public ClipboardLogEntryFilter ActiveFilter { get; init; }

	/// <summary>
	/// Whether the window stays open on focus loss and after a restore.
	/// </summary>
	public bool KeepOpen { get; init; }
	#endregion
}
