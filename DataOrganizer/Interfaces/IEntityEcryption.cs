using DataOrganizer.DTO.Encryption;
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
	/// Handles password input for encryption/decryption files in folder.
	/// </summary>
	Task<PasswordMatchResult> HandlePasswordInputAsync(
		PasswordBox view,
		EditorViewModel viewModel,
		HandlePasswordInputParameters inputParameters,
		CancellationToken token = default);

	/// <summary>
	/// Takes a password for encryption/decryption files in folder.
	/// </summary>
	Task TakeCryptPasswordAsync(
		EditorViewModel viewModel,
		TakeCryptPasswordParameters inputParameters,
		CancellationToken token = default);
	#endregion
}
