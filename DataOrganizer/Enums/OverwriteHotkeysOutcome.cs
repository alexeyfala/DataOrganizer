namespace DataOrganizer.Enums;

/// <summary>
/// Outcome of assigning a new sequence of hotkeys to a file.
/// </summary>
public enum OverwriteHotkeysOutcome
{
	SameHotkeys,
	AlreadyInUse,
	EmptySequence,
	Rewritten,
	ExceptionThrown
}
