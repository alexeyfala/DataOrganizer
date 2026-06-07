using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

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
	/// Decrypts files in folder.
	/// </summary>
	Task DecryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Decrypts contents using the session encrypted DEK.
	/// </summary>
	byte[]? DecryptSessionContents(byte[] encryptedContents, byte[] sessionEncryptedDek);

	/// <summary>
	/// Encrypts files in folder.
	/// </summary>
	Task EncryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Encrypts contents using the session encrypted DEK.
	/// </summary>
	byte[]? EncryptSessionContents(byte[] decryptedContents, byte[] sessionEncryptedDek);

	/// <summary>
	/// Returns a session identifier.
	/// </summary>
	byte[] GetSessionId();

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	void HideFolderContents(FolderModelDto folder, IEnumerable<ExplorerModelBaseDto> hierarchy);

	/// <summary>
	/// Resets the session identifier.
	/// </summary>
	void ResetSessionId();

	/// <summary>
	/// Shows file contents.
	/// </summary>
	Task<bool> ShowFileContentsAsync(FileModelDto file, CancellationToken token = default);

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default);

	/// <summary>
	/// Tries to decrypt the content, if it is decrypted.
	/// </summary>
	byte[]? TryToDecrypt(FileModelDto file, byte[] input);

	/// <summary>
	/// Tries to decrypt the content, if it has <see cref="EncryptionStatus.Encrypted" /> or <see cref="EncryptionStatus.Decrypted" /> status.
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
