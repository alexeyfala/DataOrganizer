using DataOrganizer.Enums.Clipboard;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>ClipboardLogWindow</c>.
/// </summary>
public sealed class ClipboardLogWindowSettings : PositionSizeSettings
{
	#region Properties
	/// <summary>
	/// The type filter in effect.
	/// </summary>
	public ClipboardLogEntryFilter ActiveFilter { get; init; }

	/// <summary>
	/// <c>True</c> when the window stays open on focus loss and after a restore.
	/// </summary>
	public bool KeepOpen { get; init; }
	#endregion
}
