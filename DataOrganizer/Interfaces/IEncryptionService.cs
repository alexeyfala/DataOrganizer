using DataOrganizer.Enums;
using Repository.DTO;
using System.Collections.Generic;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides encryption methods.
/// </summary>
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

	/// <summary>
	/// Encrypts/decrypts a sequence of contents.
	/// </summary>
	IEnumerable<ContentsIsValidPair> EncryptDecryptContents(
		ContentsIsValidPair[] contents,
		byte[] password,
		CryptoAction action);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedHashPassword(string)" />
	string EnhancedHashPassword(string password);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedVerify" />
	bool EnhancedVerify(string password, string passwordHash);

	/// <summary>
	/// Returns unique characteristics of the session identifier.
	/// </summary>
	byte[] GetSessionId();
	#endregion
}
