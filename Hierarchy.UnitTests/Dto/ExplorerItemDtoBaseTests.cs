using AwesomeAssertions;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using System.Collections.Generic;

namespace Hierarchy.UnitTests.Dto;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerItemDtoBase)}"" type")]
internal class ExplorerItemDtoBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindParent" />: returns the nearest matching ancestor when walking up.
	/// </summary>
	[Test]
	public void FindParent_Returns_First_Matching_Parent_Walking_Up()
	{
		// Arrange
		FolderDto grandparent = ItemDtoFactory.CreateNamedFolderDto("keep");

		FolderDto parent = ItemDtoFactory.CreateNamedFolderDto("keep");

		parent.Parent = grandparent;

		FileDto child = ItemDtoFactory.CreateNamedFileDto("child");

		child.Parent = parent;

		// Act
		FolderDto? result = child.FindParent(x => x.Name == "keep");

		// Assert
		result
			.Should()
			.BeSameAs(parent);
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindParent" />: returns null when no ancestor satisfies the condition.
	/// </summary>
	[Test]
	public void FindParent_Returns_Null_When_No_Parent_Matches()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateNamedFolderDto("parent");

		FileDto child = ItemDtoFactory.CreateNamedFileDto("child");

		child.Parent = parent;

		// Act
		FolderDto? result = child.FindParent(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindParent" />: the walk up stops at the nearest ancestor that meets the condition.
	/// </summary>
	[Test]
	public void FindParent_Stops_At_The_Nearest_Match()
	{
		// Arrange
		List<FolderDto> checkedParents = [];

		FolderDto grandparent = ItemDtoFactory.CreateNamedFolderDto("grandparent");

		FolderDto parent = ItemDtoFactory.CreateNamedFolderDto("parent");

		parent.Parent = grandparent;

		FileDto child = ItemDtoFactory.CreateNamedFileDto("child");

		child.Parent = parent;

		// Act
		child.FindParent(x =>
		{
			checkedParents.Add(x);

			return true;
		});

		// Assert
		checkedParents
			.Should()
			.Equal(parent);
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindUnlockedKeeper" />: an object that is not decrypted gets no keeper, even under
	/// one.
	/// </summary>
	[Test]
	public void FindUnlockedKeeper_Returns_Null_When_The_Object_Is_Not_Decrypted(
		[Values(EncryptionStatus.None, EncryptionStatus.Encrypted)] EncryptionStatus status)
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: status);

		file.Parent = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		// Act
		FolderDto? result = file.FindUnlockedKeeper();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindUnlockedKeeper" />: a decrypted object gets the keeper it belongs to.
	/// </summary>
	[Test]
	public void FindUnlockedKeeper_Returns_The_Keeper_Of_A_Decrypted_Object()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		FileDto file = ItemDtoFactory.CreateFileDto(encryptionStatus: EncryptionStatus.Decrypted);

		file.Parent = keeper;

		// Act
		FolderDto? result = file.FindUnlockedKeeper();

		// Assert
		result
			.Should()
			.BeSameAs(keeper);
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.GetAllParents" />: returns an empty sequence when there is no parent.
	/// </summary>
	[Test]
	public void GetAllParents_Returns_Empty_When_There_Is_No_Parent()
	{
		// Arrange
		FileDto orphan = ItemDtoFactory.CreateNamedFileDto("orphan");

		// Act
		List<FolderDto> result = [.. orphan.GetAllParents()];

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.GetAllParents" />: returns the ancestors ordered from nearest to root.
	/// </summary>
	[Test]
	public void GetAllParents_Returns_Parents_From_Nearest_To_Root()
	{
		// Arrange
		FolderDto grandparent = ItemDtoFactory.CreateNamedFolderDto("grandparent");

		FolderDto parent = ItemDtoFactory.CreateNamedFolderDto("parent");

		parent.Parent = grandparent;

		FileDto child = ItemDtoFactory.CreateNamedFileDto("child");

		child.Parent = parent;

		// Act
		List<FolderDto> result = [.. child.GetAllParents()];

		// Assert
		result
			.Should()
			.Equal(parent, grandparent);
	}
	#endregion
}
