using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions.Hierarchy;

namespace DataOrganizer.Helpers.Hierarchy;

/// <summary>
/// The rules that say when a folder can be put under a password, taken out of it or given a new password.
/// </summary>
public static class FolderProtectionRules
{
	#region Methods
	/// <summary>
	/// <c>True</c> when the folder keeps a password.
	/// </summary>
	public static bool CanChangePassword(FolderDto folder) => folder.IsPasswordKeeper();

	/// <summary>
	/// <c>True</c> when the folder keeps a password and it and every child of it are encrypted.
	/// </summary>
	public static bool CanDecrypt(FolderDto folder)
	{
		return folder.IsPasswordKeeper()
			&& folder.EncryptionStatus == EncryptionStatus.Encrypted
			&& folder.Children.AllBy(x => x.EncryptionStatus == EncryptionStatus.Encrypted);
	}

	/// <summary>
	/// <c>True</c> when the folder belongs to no password keeper, itself included, and holds no protected contents.
	/// </summary>
	public static bool CanEncrypt(FolderDto folder)
	{
		return folder.FindPasswordKeeper() is null
			&& !folder.AnyChild(x => x.EncryptionStatus != EncryptionStatus.None);
	}
	#endregion
}
