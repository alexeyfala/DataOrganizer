using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.ViewModels;
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
	Task ChangePasswordAsync(
		FolderModelDto dto,
		EditorViewModel viewModel,
		CancellationToken token = default);

	/// <summary>
	/// Decrypts files in folder.
	/// </summary>
	Task DecryptFolderAsync(
		FolderModelDto folder,
		EditorViewModel viewModel,
		CancellationToken token = default);

	/// <summary>
	/// Decrypts contents using the session encrypted DEK.
	/// </summary>
	bool DecryptSessionContents(
		byte[] encryptedContents,
		byte[] sessionEncryptedDek,
		out byte[] decryptedContents);

	/// <summary>
	/// Encrypts/decrypts files in folder.
	/// </summary>
	Task<FolderEncryptionResult> EncryptDecryptFolderAsync(
		EditorViewModel viewModel,
		FolderEncryptionParameters parameters,
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
	/// Handles password input for encryption/decryption files in folder.
	/// </summary>
	Task<HandlePasswordResult> HandlePasswordInputAsync(
		string? password,
		EditorViewModel viewModel,
		HandlePasswordParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Hides file contents.
	/// </summary>
	Task HideFileContentsAsync(
		FileModelDto file,
		EditorViewModel viewModel,
		CancellationToken token = default);

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	Task HideFolderContentsAsync(
		FolderModelDto folder,
		EditorViewModel viewModel,
		CancellationToken token = default);

	/// <summary>
	/// Requests a password for encryption/decryption files in folder.
	/// </summary>
	Task RequestPasswordAsync(
		EditorViewModel viewModel,
		FolderModelDto folder,
		CryptoAction action,
		CancellationToken token = default);

	/// <summary>
	/// Resets the session identifier.
	/// </summary>
	void ResetSessionId();

	/// <summary>
	/// Shows file contents.
	/// </summary>
	Task ShowFileContentsAsync(
		FileModelDto file,
		EditorViewModel viewModel,
		CancellationToken token = default);

	/// <summary>
	/// Updates the database.
	/// </summary>
	Task UpdateDatabaseAsync(
		UpdateDatabaseParameters parameters,
		EditorViewModel viewModel,
		CancellationToken token = default);
	#endregion
}
