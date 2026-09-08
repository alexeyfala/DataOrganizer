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
	/// Creates a random DEK (Data Encryption Key) in pinned storage the caller owns.
	/// </summary>
	PinnedBuffer CreateRandomDek();

	/// <summary>
	/// Decrypts a wrapped DEK using a password (runs KDF); the key is returned in pinned storage
	/// the caller owns.
	/// </summary>
	/// <exception cref="InvalidCredentialException">The password, or the cost and the salt the wrapper records, is not the one the wrapper was written with.</exception>
	/// <exception cref="AuthenticationTagMismatchException">The password fits, so the wrapped key is damaged.</exception>
	/// <exception cref="CryptographicException">The wrapper is of another format or size, the recorded derivation cost is unsupported, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input or the password is absent.</exception>
	PinnedBuffer Decrypt(
		byte[] input,
		PinnedBuffer password,
		ContentIdentity identity);

	/// <summary>
	/// Decrypts a sequence of contents using a DEK directly; every item is bound to the contents purpose.
	/// </summary>
	IEnumerable<ContentsIsValidPair> DecryptContents(ContentsIsValidPair[] contents, PinnedBuffer dek);

	/// <summary>
	/// Decrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The key or the associated data does not fit the input, or the input has been altered.</exception>
	/// <exception cref="CryptographicException">The data is damaged, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input or the key is absent.</exception>
	byte[] DecryptWithDek(
		byte[] input,
		PinnedBuffer dek,
		ContentIdentity identity);

	/// <summary>
	/// Unwraps the session encrypted DEK using a session identifier (runs HKDF); the key is returned
	/// in pinned storage the caller owns.
	/// </summary>
	/// <exception cref="AuthenticationTagMismatchException">The key or the associated data does not fit the input, or the input has been altered.</exception>
	/// <exception cref="CryptographicException">The wrapper is of another format or size, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input or the session identifier is absent.</exception>
	PinnedBuffer DecryptWithSessionId(
		byte[] input,
		PinnedBuffer sessionId,
		ContentIdentity identity);

	/// <summary>
	/// Encrypts a DEK using a password (runs KDF); the input is a single key and nothing else.
	/// </summary>
	/// <exception cref="CryptographicException">The input is not the size of a key, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The key or the password is absent.</exception>
	byte[] Encrypt(
		PinnedBuffer dek,
		PinnedBuffer password,
		ContentIdentity identity);

	/// <summary>
	/// Encrypts a sequence of contents using a DEK directly; every item is bound to the contents purpose.
	/// </summary>
	IEnumerable<ContentsIsValidPair> EncryptContents(ContentsIsValidPair[] contents, PinnedBuffer dek);

	/// <summary>
	/// Encrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input or the key is absent.</exception>
	byte[] EncryptWithDek(
		byte[] input,
		PinnedBuffer dek,
		ContentIdentity identity);

	/// <summary>
	/// Wraps a DEK with a session identifier (runs HKDF), for the length of a session.
	/// </summary>
	/// <exception cref="CryptographicException">The input is not the size of a key, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The key or the session identifier is absent.</exception>
	byte[] EncryptWithSessionId(
		PinnedBuffer dek,
		PinnedBuffer sessionId,
		ContentIdentity identity);

	/// <summary>
	/// Wraps the DEK with the password again when the wrapper records a derivation cost other than
	/// the current one; <c>null</c> when the recorded cost is current or cannot be read.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The wrapper, the key or the password is absent.</exception>
	byte[]? RewrapIfOutdated(
		byte[] wrapped,
		PinnedBuffer dek,
		PinnedBuffer password,
		ContentIdentity identity);
	#endregion
}
