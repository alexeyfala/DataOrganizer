using System.Collections.Generic;

namespace DataOrganizer.DTO.Clipboard.Persistence;

/// <summary>
/// Root container for the persisted clipboard log (the plaintext payload that is encrypted on disk).
/// </summary>
public sealed class PersistedClipboardLog
{
	#region Data
	/// <summary>
	/// Current payload schema version. Bump when the persisted layout changes.
	/// </summary>
	public const int CurrentVersion = 1;
	#endregion

	#region Properties
	/// <summary>
	/// Log entries, newest first.
	/// </summary>
	public List<PersistedClipboardEntryBase> Entries { get; set; } = [];

	/// <summary>
	/// Payload schema version (see <see cref="CurrentVersion" />).
	/// </summary>
	public int Version { get; set; } = CurrentVersion;
	#endregion
}
