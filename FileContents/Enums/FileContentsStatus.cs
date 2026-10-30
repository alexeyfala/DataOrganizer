namespace FileContents.Enums;

/// <summary>
/// What became of the stored contents of a file on reading.
/// </summary>
public enum FileContentsStatus
{
	/// <summary>
	/// The contents are read and, when protected, decrypted.
	/// </summary>
	Opened,

	/// <summary>
	/// The stored contents failed their check.
	/// </summary>
	Damaged,

	/// <summary>
	/// The contents stay encrypted: no password was given, or the key did not open them.
	/// </summary>
	Closed
}
