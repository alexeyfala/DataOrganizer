using DataOrganizer.Dto.Entities;
using DataOrganizer.Helpers.Security;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Encryption;

/// <summary>
/// Puts a folder under a password, takes it out and changes the password of a protected one.
/// </summary>
public interface IFolderProtection
{
	#region Methods
	/// <summary>
	/// Changes the password. The data encryption key stays the same, so a surviving copy of the old
	/// wrapper keeps opening the contents with the old password, those written after the change too.
	/// </summary>
	Task ChangePasswordAsync(FolderDto folder, CancellationToken token = default);

	/// <summary>
	/// Decrypts the files in a folder.
	/// </summary>
	Task DecryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Encrypts the files in a folder with a password asked for.
	/// </summary>
	Task EncryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		CancellationToken token = default);

	/// <summary>
	/// Encrypts the files in a folder with the given password; an empty one changes nothing.
	/// </summary>
	Task EncryptFolderAsync(
		FolderDto folder,
		FileDto[] files,
		PinnedSecret password,
		CancellationToken token = default);
	#endregion
}
