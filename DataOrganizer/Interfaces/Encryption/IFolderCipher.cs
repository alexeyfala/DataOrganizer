using DataOrganizer.Dto.Entities;
using DataOrganizer.Helpers.Security;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Encrypts and decrypts the files of a folder and wraps its key with a new password, with the key or the password
/// given; reports the failures.
/// </summary>
public interface IFolderCipher
{
	#region Methods
	/// <summary>
	/// Wraps the data encryption key of the keeper with a new password; the key itself stays the same.
	/// </summary>
	Task ChangePasswordAsync(
		FolderDto folder,
		PinnedBuffer dek,
		PinnedSecret newPassword,
		CancellationToken token = default);

	/// <summary>
	/// Decrypts the files of the keeper with its unwrapped data encryption key.
	/// </summary>
	Task DecryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		PinnedBuffer dek,
		CancellationToken token = default);

	/// <summary>
	/// Encrypts the files of the folder with a new data encryption key wrapped by the password.
	/// </summary>
	Task EncryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		PinnedSecret password,
		CancellationToken token = default);
	#endregion
}
