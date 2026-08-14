using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Provides methods for encrypting entities.
/// </summary>
public interface IEntityEncryption
{
	#region Methods
	/// <summary>
	/// Changes the password.
	/// </summary>
	Task ChangePasswordAsync(FolderModelDto dto, CancellationToken token = default);

	/// <summary>
	/// Decrypts the content of a file whose keeper is unlocked. Empty content is stored as is,
	/// so it is handed back untouched.
	/// </summary>
	/// <exception cref="InvalidOperationException">The file has no password keeper, or its keeper is locked.</exception>
	/// <exception cref="AuthenticationTagMismatchException">The key or the purpose does not fit the content, or the content has been altered.</exception>
	byte[] Decrypt(FileModelDto file, byte[] input);

	/// <summary>
	/// Decrypts files in folder.
	/// </summary>
	Task DecryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Encrypts files in folder.
	/// </summary>
	Task EncryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Hides contents of the whole hierarchy.
	/// </summary>
	void HideAllContents(IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Hides file contents.
	/// </summary>
	void HideFileContents(FileModelDto file);

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	void HideFolderContents(FolderModelDto folder);

	/// <summary>
	/// Shows file contents.
	/// </summary>
	Task<bool> ShowFileContentsAsync(FileModelDto file, CancellationToken token = default);

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default);

	/// <summary>
	/// Tries to decrypt the content, if it has <see cref="EncryptionStatus.Encrypted" /> or <see cref="EncryptionStatus.Decrypted" /> status.
	/// Empty content is handed back untouched, without asking for a password.
	/// </summary>
	Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		string header,
		CancellationToken token = default);

	/// <summary>
	/// Updates the database.
	/// </summary>
	Task<UpdateDatabaseResult> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default);
	#endregion
}
