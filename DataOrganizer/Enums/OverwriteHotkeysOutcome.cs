namespace DataOrganizer.Enums;

/// <summary>
/// Outcome of assigning a new sequence of hotkeys to a file.
/// </summary>
public enum OverwriteHotkeysOutcome
{
	/// <summary>
	/// The file already holds this very sequence, so nothing was written.
	/// </summary>
	SameHotkeys,

	/// <summary>
	/// The sequence belongs to another file, so nothing was written.
	/// </summary>
	AlreadyInUse,

	/// <summary>
	/// The new sequence is empty: the previous hotkeys are gone and nothing took their place.
	/// </summary>
	EmptySequence,

	/// <summary>
	/// The previous hotkeys are replaced by the new sequence.
	/// </summary>
	Rewritten,

	/// <summary>
	/// The write failed with an exception; the previous hotkeys may already be gone.
	/// </summary>
	ExceptionThrown
}
