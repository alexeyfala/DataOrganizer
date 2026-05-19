namespace DataOrganizer.Enums;

/// <summary>
/// The status of encryption.
/// </summary>
public enum EncryptionStatus
{
	/// <summary>
	/// Original data.
	/// </summary>
	None,

	/// <summary>
	/// The data has been decrypted.
	/// </summary>
	Decrypted,

	/// <summary>
	/// The data is encrypted
	/// </summary>
	Encrypted
}
