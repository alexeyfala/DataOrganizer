using DataOrganizer.Helpers.Security;
using Repository.DTO;
using System;
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
	/// <exception cref="CryptographicException">The data is damaged, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] Decrypt(
		byte[] input,
		PinnedBuffer password,
		byte[] associatedData);

	/// <summary>
	/// Decrypts a sequence of contents using a DEK directly; every item is bound to the contents purpose.
	/// </summary>
	IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, byte[] dek);

	/// <summary>
	/// Decrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The key or the associated data does not fit the input, or the input has been altered.</exception>
	/// <exception cref="CryptographicException">The data is damaged, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] DecryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData);

	/// <summary>
	/// Decrypts data using a session identifier (runs HKDF). For unwrap of the session encrypted DEK.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The key or the associated data does not fit the input, or the input has been altered.</exception>
	/// <exception cref="CryptographicException">The data is damaged, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] DecryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData);

	/// <summary>
	/// Encrypts data using a password (runs KDF). For wrap/unwrap of DEK.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] Encrypt(
		byte[] input,
		PinnedBuffer password,
		byte[] associatedData);

	/// <summary>
	/// Encrypts a sequence of contents using a DEK directly; every item is bound to the contents purpose.
	/// </summary>
	IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, byte[] dek);

	/// <summary>
	/// Encrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] EncryptWithDek(
		byte[] input,
		byte[] dek,
		byte[] associatedData);

	/// <summary>
	/// Encrypts data using a session identifier (runs HKDF). For wrap of the DEK within a session.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] EncryptWithSessionId(
		byte[] input,
		byte[] sessionId,
		byte[] associatedData);
	#endregion
}
