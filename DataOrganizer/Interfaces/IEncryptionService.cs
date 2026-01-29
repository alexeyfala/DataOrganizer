namespace DataOrganizer.Interfaces;

public interface IEncryptionService
{
	#region Methods
	/// <summary>
	/// Decrypts data.
	/// </summary>
	bool Decrypt(byte[] input, byte[] password, out byte[] output);

	/// <summary>
	/// Encrypts data.
	/// </summary>
	bool Encrypt(byte[] input, byte[] password, out byte[] output);
	#endregion
}
