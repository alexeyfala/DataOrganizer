using Repository.DTO;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides encryption methods.
/// </summary>
public interface IEncryptionService
{
	#region Methods
	/// <summary>
	/// Creates a random DEK (Data Encryption Key).
	/// </summary>
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
	string HashPassword(char[] password);

	/// <summary>
	/// Rewraps the DEK (Data Encryption Key) with new password.
	/// </summary>
	byte[]? RewrapDek(
		byte[] wrappedDek,
		byte[] oldPassword,
		byte[] newPassword);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedVerify" />
	bool VerifyPassword(char[] password, string hash);
	#endregion
}
