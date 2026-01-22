namespace DataOrganizer.Enums;

public enum OverwriteHotkeysResult : byte
{
	SameHotkeys,
	AlreadyInUse,
	EmptySequence,
	Rewritten,
	ExceptionThrown
}
