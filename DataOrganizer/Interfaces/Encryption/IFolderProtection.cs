using DataOrganizer.DTO.Entities;
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
	Task ChangePasswordAsync(FolderModelDto folder, CancellationToken token = default);

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
	#endregion
}
