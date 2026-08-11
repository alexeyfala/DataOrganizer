using Repository.DTO;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography;

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
	/// <exception cref="InvalidCredentialException">The password does not fit the data.</exception>
	/// <exception cref="CryptographicException">The data is damaged or the operation failed.</exception>
	byte[] Decrypt(
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
	/// <exception cref="AuthenticationTagMismatchException">The data does not belong here or has been altered.</exception>
	/// <exception cref="CryptographicException">The data is damaged or the operation failed.</exception>
	byte[] DecryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData);

	/// <summary>
	/// Decrypts data using a session identifier (runs HKDF). For unwrap of the session encrypted DEK.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The data does not belong here or has been altered.</exception>
	/// <exception cref="CryptographicException">The data is damaged or the operation failed.</exception>
	byte[] DecryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData);

	/// <summary>
	/// Encrypts data using a password (runs KDF). For wrap/unwrap of DEK.
	/// </summary>
	/// <exception cref="CryptographicException">The operation failed.</exception>
	byte[] Encrypt(
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
	/// <exception cref="CryptographicException">The operation failed.</exception>
	byte[] EncryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData);

	/// <summary>
	/// Encrypts data using a session identifier (runs HKDF). For wrap of the DEK within a session.
	/// </summary>
	/// <exception cref="CryptographicException">The operation failed.</exception>
	byte[] EncryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedHashPassword(string)" />
	string HashPassword(char[] password);

	/// <summary>
	/// Rewraps the DEK (Data Encryption Key) with new password.
	/// </summary>
	/// <exception cref="InvalidCredentialException">The old password does not fit the wrapped key.</exception>
	/// <exception cref="CryptographicException">The wrapped key is damaged or the operation failed.</exception>
	byte[] RewrapDek(
		byte[] wrappedDek,
		byte[] oldPassword,
		byte[] newPassword,
		byte[] associatedData);

	/// <inheritdoc cref="BCrypt.Net.BCrypt.EnhancedVerify" />
	bool VerifyPassword(char[] password, string hash);
	#endregion
}
