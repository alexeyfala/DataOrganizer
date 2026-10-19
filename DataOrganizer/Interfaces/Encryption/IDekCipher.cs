using DataOrganizer.Helpers.Security;
using Repository.Dto;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Encrypts and decrypts contents with a data encryption key.
/// </summary>
public interface IDekCipher
{
	#region Methods
	/// <summary>
	/// Decrypts a sequence of contents using a DEK directly; every item is bound to the contents purpose.
	/// </summary>
	IEnumerable<ValidatedContents> DecryptContents(ValidatedContents[] contents, PinnedBuffer dek);

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
	/// Encrypts a sequence of contents using a DEK directly; every item is bound to the contents purpose.
	/// </summary>
	IEnumerable<ValidatedContents> EncryptContents(ValidatedContents[] contents, PinnedBuffer dek);

	/// <summary>
	/// Encrypts data using a DEK directly (no KDF). For content encryption.
	/// </summary>
	/// <exception cref="CryptographicException">The key material is unusable or the operation failed.</exception>
	/// <exception cref="ArgumentNullException">The input or the key is absent.</exception>
	byte[] EncryptWithDek(
		byte[] input,
		PinnedBuffer dek,
		ContentIdentity identity);
	#endregion
}
