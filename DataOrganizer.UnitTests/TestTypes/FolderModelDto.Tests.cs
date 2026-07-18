using AwesomeAssertions;
using DataOrganizer.DTO.Entities;
using Entities.Enums;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(FolderModelDto)}"" type")]
internal class FolderModelDtoTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderModelDto.AnyChild" />: returns false when no descendant satisfies the condition.
	/// </summary>
	[Test]
	public void AnyChild_Returns_False_When_No_Child_Matches()
	{
		// Arrange
		FolderModelDto root = CreateFolder();

		AddChild(root, CreateFile("a"));

		// Act
		bool result = root.AnyChild(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderModelDto.AnyChild" />: returns true when a deeply nested descendant matches.
	/// </summary>
	[Test]
	public void AnyChild_Returns_True_When_A_Nested_Child_Matches()
	{
		// Arrange
		FolderModelDto root = CreateFolder();

		FolderModelDto sub = CreateFolder("sub");

		AddChild(root, sub);

		AddChild(sub, CreateFile("target"));

		// Act
		bool result = root.AnyChild(x => x.Name == "target");

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FolderModelDto.AnyFile" />: returns false when only folders match the condition.
	/// </summary>
	[Test]
	public void AnyFile_Returns_False_When_Only_Folders_Match()
	{
		// Arrange
		FolderModelDto root = CreateFolder();

		AddChild(root, CreateFolder("target"));

		// Act
		bool result = root.AnyFile(file => file.Name == "target");

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderModelDto.AnyFile" />: returns true when a nested file satisfies the condition.
	/// </summary>
	[Test]
	public void AnyFile_Returns_True_When_A_Nested_File_Matches()
	{
		// Arrange
		FolderModelDto root = CreateFolder();

		FolderModelDto sub = CreateFolder("sub");

		AddChild(root, sub);

		AddChild(sub, CreateFile("target"));

		// Act
		bool result = root.AnyFile(file => file.Name == "target");

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="FolderModelDto.FindPasswordKeeperOrSelf" />: returns null when no keeper exists in the chain.
	/// </summary>
	[Test]
	public void FindPasswordKeeperOrSelf_Returns_Null_When_No_Keeper_In_Chain()
	{
		// Arrange
		FolderModelDto parent = CreateFolder("parent");

		FolderModelDto child = CreateFolder("child");

		AddChild(parent, child);

		// Act
		FolderModelDto? result = child.FindPasswordKeeperOrSelf();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FolderModelDto.FindPasswordKeeperOrSelf" />: returns the nearest parent keeper when self is not one.
	/// </summary>
	[Test]
	public void FindPasswordKeeperOrSelf_Returns_Parent_Keeper_When_Self_Is_Not()
	{
		// Arrange
		FolderModelDto keeper = CreateFolder("keeper", encryptedDek: [1], passwordHash: "hash");

		FolderModelDto child = CreateFolder("child");

		AddChild(keeper, child);

		// Act
		FolderModelDto? result = child.FindPasswordKeeperOrSelf();

		// Assert
		result
			.Should()
			.BeSameAs(keeper);
	}

	/// <summary>
	/// <see cref="FolderModelDto.FindPasswordKeeperOrSelf" />: returns itself when self is a password keeper.
	/// </summary>
	[Test]
	public void FindPasswordKeeperOrSelf_Returns_Self_When_Self_Is_Password_Keeper()
	{
		// Arrange
		FolderModelDto keeper = CreateFolder("keeper", encryptedDek: [1], passwordHash: "hash");

		// Act
		FolderModelDto? result = keeper.FindPasswordKeeperOrSelf();

		// Assert
		result
			.Should()
			.BeSameAs(keeper);
	}

	/// <summary>
	/// <see cref="FolderModelDto.GetAllChildren" />: returns every descendant flattened into a single sequence.
	/// </summary>
	[Test]
	public void GetAllChildren_Returns_All_Descendants_Flattened()
	{
		// Arrange
		FolderModelDto root = CreateFolder();

		FolderModelDto sub = CreateFolder("sub");

		FileModelDto nested = CreateFile("nested");

		FileModelDto top = CreateFile("top");

		AddChild(root, sub);

		AddChild(sub, nested);

		AddChild(root, top);

		// Act
		List<ExplorerModelBaseDto> result = [.. root.GetAllChildren()];

		// Assert
		result
			.Should()
			.HaveCount(3);

		result
			.Should()
			.Contain([sub, nested, top]);
	}

	/// <summary>
	/// <see cref="FolderModelDto.GetFiles" />: returns only the files matching the condition across the whole subtree.
	/// </summary>
	[Test]
	public void GetFiles_Returns_Only_Matching_Files()
	{
		// Arrange
		FolderModelDto root = CreateFolder();

		FileModelDto keptTop = CreateFile("keep");

		FolderModelDto sub = CreateFolder("sub");

		FileModelDto keptNested = CreateFile("keep");

		FileModelDto skipped = CreateFile("skip");

		AddChild(root, keptTop);

		AddChild(root, sub);

		AddChild(sub, keptNested);

		AddChild(sub, skipped);

		// Act
		List<FileModelDto> result = [.. root.GetFiles(file => file.Name == "keep")];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.Contain([keptTop, keptNested]);
	}

	/// <summary>
	/// <see cref="FolderModelDto.IsPasswordKeeper" />: returns false when the encrypted DEK is empty.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_False_When_Dek_Empty()
	{
		// Arrange
		FolderModelDto folder = CreateFolder("folder", encryptedDek: [], passwordHash: "hash");

		// Act
		bool result = folder.IsPasswordKeeper();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderModelDto.IsPasswordKeeper" />: returns false when the encrypted DEK is missing.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_False_When_Dek_Missing()
	{
		// Arrange
		FolderModelDto folder = CreateFolder("folder", passwordHash: "hash");

		// Act
		bool result = folder.IsPasswordKeeper();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderModelDto.IsPasswordKeeper" />: returns false when the password hash is missing.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_False_When_Hash_Missing()
	{
		// Arrange
		FolderModelDto folder = CreateFolder("folder", encryptedDek: [1]);

		// Act
		bool result = folder.IsPasswordKeeper();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderModelDto.IsPasswordKeeper" />: returns true when both the encrypted DEK and the hash are present.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_True_When_Dek_And_Hash_Present()
	{
		// Arrange
		FolderModelDto folder = CreateFolder("folder", encryptedDek: [1], passwordHash: "hash");

		// Act
		bool result = folder.IsPasswordKeeper();

		// Assert
		result
			.Should()
			.BeTrue();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Links a child to a parent folder, wiring both the parent reference and the children collection.
	/// </summary>
	private static void AddChild(FolderModelDto parent, ExplorerModelBaseDto child)
	{
		child.Parent = parent;

		parent.Children.Add(child);
	}

	/// <summary>
	/// Creates a file DTO with the required base members populated.
	/// </summary>
	private static FileModelDto CreateFile(string name = "") => new()
	{
		Id = Guid.NewGuid(),
		Index = 0,
		Name = name,
		CreatedDate = DateTime.UtcNow,
		UpdatedDate = DateTime.UtcNow,
		EntityType = EntityType.File
	};

	/// <summary>
	/// Creates a folder DTO with the required base members populated and optional password-keeper data.
	/// </summary>
	private static FolderModelDto CreateFolder(
		string name = "",
		byte[]? encryptedDek = null,
		string? passwordHash = null) => new()
		{
			Id = Guid.NewGuid(),
			Index = 0,
			Name = name,
			CreatedDate = DateTime.UtcNow,
			UpdatedDate = DateTime.UtcNow,
			EntityType = EntityType.Folder,
			EncryptedDek = encryptedDek,
			PasswordHash = passwordHash
		};
	#endregion
}
