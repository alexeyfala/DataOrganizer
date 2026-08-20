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
	/// Decrypts a wrapped DEK using a password (runs KDF); the input holds a single key.
	/// </summary>
	/// <exception cref="InvalidCredentialException">The password, or the cost and the salt the wrapper records, is not the one the wrapper was written with.</exception>
	/// <exception cref="AuthenticationTagMismatchException">The password fits, so the wrapped key is damaged.</exception>
	/// <exception cref="CryptographicException">The wrapper is of another format or size, the recorded derivation cost is unsupported, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] Decrypt(
		byte[] input,
		PinnedBuffer password,
		ContentIdentity identity);

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
		ContentIdentity identity);

	/// <summary>
	/// Decrypts data using a session identifier (runs HKDF). For unwrap of the session encrypted DEK.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The key or the associated data does not fit the input, or the input has been altered.</exception>
	/// <exception cref="CryptographicException">The data is damaged, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] DecryptWithSessionId(
		byte[] input,
		PinnedBuffer sessionId,
		ContentIdentity identity);

	/// <summary>
	/// Encrypts a DEK using a password (runs KDF); the input is a single key and nothing else.
	/// </summary>
	/// <exception cref="CryptographicException">The input is not the size of a key, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] Encrypt(
		byte[] input,
		PinnedBuffer password,
		ContentIdentity identity);

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
		ContentIdentity identity);

	/// <summary>
	/// Encrypts data using a session identifier (runs HKDF). For wrap of the DEK within a session.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input is absent.</exception>
	byte[] EncryptWithSessionId(
		byte[] input,
		PinnedBuffer sessionId,
		ContentIdentity identity);

	/// <summary>
	/// Wraps the DEK with the password again when the wrapper records a derivation cost other than
	/// the current one; <c>null</c> when the recorded cost is current or cannot be read.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The wrapper is absent.</exception>
	byte[]? RewrapIfOutdated(
		byte[] wrapped,
		byte[] dek,
		PinnedBuffer password,
		ContentIdentity identity);
	#endregion
}
