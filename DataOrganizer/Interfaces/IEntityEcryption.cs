using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
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
		FileModelDto[] files,
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
	/// Encryptd files in folder.
	/// </summary>
	Task EncryptFolderAsync(
		FolderModelDto folder,
		FileModelDto[] files,
		EditorViewModel viewModel,
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
	/// Hides file contents.
	/// </summary>
	Task HideFileContentsAsync(
		FileModelDto file,
		EditorViewModel viewModel,
		CancellationToken token = default);

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	void HideFolderContents(FolderModelDto folder, EditorViewModel viewModel);

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
	/// Shows file contents in folder.
	/// </summary>
	Task ShowFolderContentsAsync(
		FolderModelDto folder,
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
