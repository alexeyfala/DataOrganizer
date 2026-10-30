namespace FileContents.Enums;

/// <summary>
/// What the stored contents of a file hold when read as text.
/// </summary>
public enum FileTextStatus
{
	/// <summary>
	/// The contents hold a text.
	/// </summary>
	Text,

	/// <summary>
	/// The contents hold no text at all.
	/// </summary>
	Empty,

	/// <summary>
	/// The contents are bytes that are not text.
	/// </summary>
	NotText,

	/// <summary>
	/// The stored contents failed their check.
	/// </summary>
	Damaged,

	/// <summary>
	/// The contents stay encrypted: no password was given, or the key did not open them.
	/// </summary>
	Closed
}
