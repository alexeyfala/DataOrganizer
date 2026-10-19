using DataOrganizer.Dto.Entities;
using DataOrganizer.Helpers.Security;
using System;
using System.Security.Cryptography;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Encrypts and decrypts contents with the session keys of unlocked keepers.
/// </summary>
public interface IContentCipher
{
	#region Methods
	/// <summary>
	/// Decrypts the content of a file whose keeper is unlocked. Empty content is stored as is,
	/// so it is handed back untouched.
	/// </summary>
	/// <exception cref="InvalidOperationException">The file has no password keeper, or its keeper is locked.</exception>
	/// <exception cref="AuthenticationTagMismatchException">The key or the purpose does not fit the content, or the content has been altered.</exception>
	byte[] Decrypt(FileDto file, byte[] input);

	/// <summary>
	/// Decrypts contents held under an unlocked keeper; <c>null</c> reports a refusal, which is logged.
	/// </summary>
	byte[]? TryDecrypt(Guid keeperId, ContentIdentity identity, byte[] input);

	/// <summary>
	/// Encrypts contents under an unlocked keeper; <c>null</c> reports a refusal, which is logged.
	/// </summary>
	byte[]? TryEncrypt(Guid keeperId, ContentIdentity identity, byte[] input);

	#endregion
}
