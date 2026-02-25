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
	byte[] CreateRandomDek();

	/// <summary>
	/// Decrypts data.
	/// </summary>
	bool Decrypt(
		byte[] input,
		byte[] password,
		out byte[] output);

	/// <summary>
	/// Decrypts a sequence of contents.
	/// </summary>
	IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] password);

	/// <summary>
	/// Encrypts data.
	/// </summary>
	bool Encrypt(
		byte[] input,
		byte[] password,
		out byte[] output);

	/// <summary>
	/// Encrypts a sequence of contents.
	/// </summary>
	IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] password);

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
	/// Rewraps the DEK (Data Encryption Key) with new password.
	/// </summary>
	bool RewrapDek(
		byte[] wrappedDek,
		byte[] oldPassword,
		byte[] newPassword,
		out byte[] newWrappedDek);
	#endregion
}
