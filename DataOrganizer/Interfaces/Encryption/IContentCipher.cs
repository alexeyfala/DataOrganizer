using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Opens the protected contents of a file, asking for a password when the keeper is locked.
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
	byte[] Decrypt(FileModelDto file, byte[] input);

	/// <summary>
	/// Tries to decrypt the content, if it has <see cref="EncryptionStatus.Encrypted" /> or <see cref="EncryptionStatus.Decrypted" /> status.
	/// Empty content is handed back untouched, without asking for a password.
	/// </summary>
	Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		string header,
		CancellationToken token = default);
	#endregion
}
