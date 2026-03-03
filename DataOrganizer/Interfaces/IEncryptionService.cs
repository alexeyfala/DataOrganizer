using Repository.DTO;
using System.Collections.Generic;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides encryption methods.
/// </summary>
public interface IEncryptionService
{
	#region Methods
	byte[] CreateRandomDek();

	/// <summary>
	/// Decrypts data.
	/// </summary>
	byte[]? Decrypt(byte[] input, byte[] password);

	/// <summary>
	/// Decrypts a sequence of contents.
	/// </summary>
	IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] password);

	/// <summary>
	/// Encrypts data.
	/// </summary>
	byte[]? Encrypt(byte[] input, byte[] password);

	/// <summary>
	/// Encrypts a sequence of contents.
	/// </summary>
	IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] password);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedHashPassword(string)" />
	string EnhancedHashPassword(string password);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedVerify" />
	bool EnhancedVerify(string password, string passwordHash);

	/// <summary>
	/// Rewraps the DEK (Data Encryption Key) with new password.
	/// </summary>
	bool RewrapDek(
		byte[] wrappedDek,
		byte[] oldPassword,
		byte[] newPassword,
		out byte[] newWrappedDek);
	#endregion
}
