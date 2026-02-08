using DataOrganizer.DTO.Encryption;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Enums;
using DataOrganizer.ViewModels;
using DataOrganizer.Views;
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
	/// Encrypts/decrypts files in folder.
	/// </summary>
	Task<FilesEncryptionResult> EncryptDecryptAsync(
		EditorViewModel viewModel,
		EncryptDecryptFilesParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Returns a session identifier.
	/// </summary>
	byte[] GetSessionId();

	/// <summary>
	/// Handles password input for encryption/decryption files in folder.
	/// </summary>
	Task<HandlePasswordResult> HandlePasswordInputAsync(
		PasswordBox view,
		EditorViewModel viewModel,
		HandlePasswordInputParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Hides file contents in folder.
	/// </summary>
	void HideFileContents(FolderModelDto folder);

	/// <summary>
	/// Resets the session identifier.
	/// </summary>
	void ResetSessionId();

	/// <summary>
	/// Shows file contents.
	/// </summary>
	bool ShowFileContents(FolderModelDto folder, string password);

	/// <summary>
	/// Takes a password for encryption/decryption files in folder.
	/// </summary>
	Task TakePasswordAsync(
		EditorViewModel viewModel,
		FolderModelDto folder,
		CryptoAction action,
		CancellationToken token = default);
	#endregion
}
