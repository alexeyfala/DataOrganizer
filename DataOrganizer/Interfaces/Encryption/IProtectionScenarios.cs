using DataOrganizer.Dto.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Runs the protection commands of the tree whole: saves the open editors, closes the open files, protects or shows or
/// hides the contents and tells the auto-lock.
/// </summary>
public interface IProtectionScenarios
{
	#region Methods
	/// <summary>
	/// Changes the password of a password keeper once its open files are closed.
	/// </summary>
	Task ChangePasswordAsync(FolderDto folder, CancellationToken token = default);

	/// <summary>
	/// Decrypts the files of a password keeper once its open files are closed.
	/// </summary>
	Task DecryptFolderAsync(FolderDto folder, CancellationToken token = default);

	/// <summary>
	/// Encrypts the files of a folder once its open files are closed.
	/// </summary>
	Task EncryptFolderAsync(FolderDto folder, CancellationToken token = default);

	/// <summary>
	/// Hides every decrypted contents of the tree; <c>true</c> when they are hidden.
	/// </summary>
	Task<bool> HideAllContentsAsync(CancellationToken token = default);

	/// <summary>
	/// Hides the contents of a file; <c>true</c> when they are hidden.
	/// </summary>
	Task<bool> HideFileContentsAsync(FileDto file, CancellationToken token = default);

	/// <summary>
	/// Hides the contents of the files of a folder; <c>true</c> when they are hidden.
	/// </summary>
	Task<bool> HideFolderContentsAsync(FolderDto folder, CancellationToken token = default);

	/// <summary>
	/// Decrypts the contents of a file for the session; <c>true</c> when they are shown.
	/// </summary>
	Task<bool> ShowFileContentsAsync(FileDto file, CancellationToken token = default);

	/// <summary>
	/// Decrypts the contents of the files of a folder for the session; <c>true</c> when they are shown.
	/// </summary>
	Task<bool> ShowFolderContentsAsync(FolderDto folder, CancellationToken token = default);
	#endregion
}
