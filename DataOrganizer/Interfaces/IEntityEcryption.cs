using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods for encrypting entities.
/// </summary>
public interface IEntityEcryption
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
	bool DecryptSessionContents(
		byte[] encryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] decryptedContents);

	/// <summary>
	/// Encryptd files in folder.
	/// </summary>
	Task EncryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Encrypts contents using the session encrypted DEK.
	/// </summary>
	bool EncryptSessionContents(
		byte[] decryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] encryptedContents);

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
	Task ShowFileContentsAsync(FileModelDto file, CancellationToken token = default);

	/// <summary>
	/// Shows file contents in folder.
	/// </summary>
	Task ShowFolderContentsAsync(FolderModelDto folder, CancellationToken token = default);

	/// <summary>
	/// Tries to decrypt the content, if it has <see cref="EncryptionStatus.Encrypted" /> or <see cref="EncryptionStatus.Decrypted" /> status.
	/// </summary>
	Task<byte[]?> TryToDecryptContentsAsync(
		FileModelDto file,
		byte[] contents,
		CancellationToken token = default);

	/// <summary>
	/// Updates the database.
	/// </summary>
	Task<UpdateDatabaseResult> UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Tries to decrypt the content, if it is decrypted.
	/// </summary>
	bool TryToDecrypt(
		byte[] input,
		FileModelDto file,
		out byte[] output);
	#endregion
}
