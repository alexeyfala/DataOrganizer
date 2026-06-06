using System.Collections.Generic;

namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// Root container for the persisted clipboard history (the plaintext payload that is encrypted on disk).
/// </summary>
public sealed class PersistedClipboardHistory
{
	#region Data
	/// <summary>
	/// Current payload schema version. Bump when the persisted layout changes.
	/// </summary>
	public const int CurrentVersion = 1;
	#endregion

	#region Properties
	/// <summary>
	/// History entries, newest first.
	/// </summary>
	public List<PersistedClipboardEntry> Entries { get; set; } = [];

	/// <summary>
	/// Payload schema version (see <see cref="CurrentVersion" />).
	/// </summary>
	public int Version { get; set; } = CurrentVersion;
	#endregion
}
