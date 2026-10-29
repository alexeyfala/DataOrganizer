using DataProtection.Helpers;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using TestSupport.Common;

namespace DataProtection.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(FolderProtectionRules)}"" type")]
internal class FolderProtectionRulesTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderProtectionRules.CanDecrypt" />: a keeper whose contents are shown for the session is not decrypted,
	/// though its children are encrypted.
	/// </summary>
	[Test]
	[TestCase(EncryptionStatus.Encrypted, ExpectedResult = true)]
	[TestCase(EncryptionStatus.Decrypted, ExpectedResult = false)]
	public bool CanDecrypt_Allows_Only_An_Encrypted_Keeper(EncryptionStatus keeperStatus)
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto(encryptionStatus: keeperStatus);

		FileDto child = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Encrypted);

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		child.Parent = keeper;

		keeper
			.Children
			.Add(child);

		// Act
		return FolderProtectionRules.CanDecrypt(keeper);
	}

	/// <summary>
	/// <see cref="FolderProtectionRules.CanEncrypt" />: a folder outside every keeper is refused once one of its objects
	/// holds protected contents.
	/// </summary>
	[Test]
	[TestCase(EncryptionStatus.None, ExpectedResult = true)]
	[TestCase(EncryptionStatus.Encrypted, ExpectedResult = false)]
	public bool CanEncrypt_Refuses_A_Folder_With_Protected_Contents(EncryptionStatus childStatus)
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto child = ItemDtoFactory.CreateFileDto(encryptionStatus: childStatus);

		child.Parent = folder;

		folder
			.Children
			.Add(child);

		// Act
		return FolderProtectionRules.CanEncrypt(folder);
	}
	#endregion
}
