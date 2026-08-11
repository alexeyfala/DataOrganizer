using Repository.DTO;
using System.Collections.Generic;

namespace DataOrganizer.Interfaces.Encryption;

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
	/// Decrypts data using a password (runs KDF). For wrap/unwrap of DEK.
	/// </summary>
	byte[]? Decrypt(
		byte[] input,
		byte[] password,
		byte[] associatedData);

	/// <summary>
	/// Decrypts a sequence of contents using a DEK directly; each item is bound to its own identifier.
	/// </summary>
	IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] dek);

	/// <summary>
	/// Decrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	byte[]? DecryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData);

	/// <summary>
	/// Decrypts data using a session identifier (runs HKDF). For unwrap of the session encrypted DEK.
	/// </summary>
	byte[]? DecryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData);

	/// <summary>
	/// Encrypts data using a password (runs KDF). For wrap/unwrap of DEK.
	/// </summary>
	byte[]? Encrypt(
		byte[] input,
		byte[] password,
		byte[] associatedData);

	/// <summary>
	/// Encrypts a sequence of contents using a DEK directly; each item is bound to its own identifier.
	/// </summary>
	IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] dek);

	/// <summary>
	/// Encrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	byte[]? EncryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData);

	/// <summary>
	/// Encrypts data using a session identifier (runs HKDF). For wrap of the DEK within a session.
	/// </summary>
	byte[]? EncryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedHashPassword(string)" />
	string HashPassword(char[] password);

	/// <summary>
	/// Rewraps the DEK (Data Encryption Key) with new password.
	/// </summary>
	byte[]? RewrapDek(
		byte[] wrappedDek,
		byte[] oldPassword,
		byte[] newPassword,
		byte[] associatedData);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedVerify" />
	bool VerifyPassword(char[] password, string hash);
	#endregion
}
