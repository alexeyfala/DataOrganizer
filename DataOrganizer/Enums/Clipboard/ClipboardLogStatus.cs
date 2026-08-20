namespace DataOrganizer.Enums.Clipboard;

/// <summary>
/// Outcome of an attempt to unlock the persisted clipboard log.
/// </summary>
public enum ClipboardLogStatus
{
	/// <summary>
	/// The log was unlocked (existing key opened, or a new key created).
	/// </summary>
	Unlocked,

	/// <summary>
	/// An existing key could not be opened with the supplied password.
	/// </summary>
	WrongPassword,

	/// <summary>
	/// The password fits, so the key or the journal behind it is damaged; another attempt cannot help.
	/// </summary>
	Damaged,

	/// <summary>
	/// Unlocking failed for another reason (I/O error).
	/// </summary>
	Failed
}
