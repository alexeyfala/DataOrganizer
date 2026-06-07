namespace DataOrganizer.Enums.Clipboard;

/// <summary>
/// Outcome of an attempt to unlock the persisted clipboard history.
/// </summary>
public enum ClipboardHistoryUnlockStatus
{
	/// <summary>
	/// The history was unlocked (existing key opened, or a new key created).
	/// </summary>
	Unlocked,

	/// <summary>
	/// An existing key could not be opened with the supplied password.
	/// </summary>
	WrongPassword,

	/// <summary>
	/// Unlocking failed for another reason (I/O or cryptographic error).
	/// </summary>
	Failed
}
