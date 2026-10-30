namespace FileContents.Enums;

/// <summary>
/// What became of the contents of a file on writing.
/// </summary>
public enum FileContentsWriteStatus
{
	/// <summary>
	/// The contents are written and, when protected, encrypted.
	/// </summary>
	Written,

	/// <summary>
	/// Nothing is written, as the file is gone from the database.
	/// </summary>
	Missing,

	/// <summary>
	/// Nothing is written, as the session key of the keeper refused to encrypt the contents.
	/// </summary>
	Closed
}
