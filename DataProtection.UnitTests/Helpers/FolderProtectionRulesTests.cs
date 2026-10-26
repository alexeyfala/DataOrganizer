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
	#endregion
}
