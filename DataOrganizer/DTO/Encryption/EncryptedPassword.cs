namespace DataOrganizer.DTO.Encryption;

/// <summary>
/// Encrypted password.
/// </summary>
public readonly struct EncryptedPassword
{
	#region Properties
	/// <summary>
	/// Password.
	/// </summary>
	public required byte[] Password { get; init; }

	/// <summary>
	/// A sequence of random bytes.
	/// </summary>
	public required byte[] RandomBytes { get; init; }
	#endregion
}
