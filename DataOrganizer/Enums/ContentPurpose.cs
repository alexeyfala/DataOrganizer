namespace DataOrganizer.Enums;

/// <summary>
/// The field a ciphertext belongs to. Separates the domains a key is used in,
/// so a blob cannot be moved from one field to another.
/// </summary>
public enum ContentPurpose : byte
{
	/// <summary>
	/// Contents of a file.
	/// </summary>
	Contents = 1,

	/// <summary>
	/// Note of a file or of a folder.
	/// </summary>
	Note = 2,

	/// <summary>
	/// Data encryption key of a password keeper.
	/// </summary>
	Dek = 3
}
