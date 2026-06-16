namespace DataOrganizer.Enums.Clipboard;

/// <summary>
/// Describes what happened to the in-memory clipboard log, so listeners (e.g. persistence)
/// can react appropriately.
/// </summary>
public enum ClipboardLogChangeKind
{
	/// <summary>
	/// Content changed (an entry was captured, moved to the top, or trimmed).
	/// </summary>
	Updated,

	/// <summary>
	/// The user explicitly cleared the log.
	/// </summary>
	ClearedByUser,

	/// <summary>
	/// The log was cleared because tracking was turned off (not a user-initiated clear).
	/// </summary>
	ClearedForStop
}
