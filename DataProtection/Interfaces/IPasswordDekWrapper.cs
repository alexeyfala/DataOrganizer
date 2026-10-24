using DataProtection.Helpers;
using System;
using System.Security.Authentication;
using System.Security.Cryptography;

namespace DataProtection.Interfaces;

/// <summary>
/// Wraps data encryption keys with a password.
/// </summary>
public interface IPasswordDekWrapper
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
	/// Encrypts a DEK using a password (runs KDF); the input is a single key and nothing else.
	/// </summary>
	/// <exception cref="CryptographicException">The input is not the size of a key, the key material is unusable, or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The key or the password is absent.</exception>
	byte[] Encrypt(
		PinnedBuffer dek,
		PinnedBuffer password,
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
