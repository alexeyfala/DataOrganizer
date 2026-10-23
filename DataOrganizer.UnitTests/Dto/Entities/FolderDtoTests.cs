using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.UnitTests.Factories;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Dto.Entities;

[TestFixture(Description = $@"Tests of ""{nameof(FolderDto)}"" type")]
internal class FolderDtoTests
{
	#region Methods
	/// <summary>
	/// <see cref="FolderDto.AnyChild" />: returns false when no descendant satisfies the condition.
	/// </summary>
	[Test]
	public void AnyChild_Returns_False_When_No_Child_Matches()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateNamedFolderDto();

		AddChild(root, ItemDtoFactory.CreateNamedFileDto("a"));

		// Act
		bool result = root.AnyChild(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderDto.AnyChild" />: returns true when a deeply nested descendant matches.
	/// </summary>
	[Test]
	public void AnyChild_Returns_True_When_A_Nested_Child_Matches()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateNamedFolderDto();

		FolderDto sub = ItemDtoFactory.CreateNamedFolderDto("sub");

		AddChild(root, sub);

		AddChild(sub, ItemDtoFactory.CreateNamedFileDto("target"));

		// Act
		bool result = root.AnyChild(x => x.Name == "target");

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindPasswordKeeper" />: a file is never a keeper itself, so the
	/// search starts at its parent.
	/// </summary>
	[Test]
	public void FindPasswordKeeper_Of_A_File_Returns_The_Parent_Keeper()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateNamedFolderDto("keeper", encryptedDek: [1]);

		FolderDto child = ItemDtoFactory.CreateNamedFolderDto("child");

		FileDto file = ItemDtoFactory.CreateNamedFileDto("file");

		AddChild(keeper, child);

		AddChild(child, file);

		// Act
		FolderDto? result = file.FindPasswordKeeper();

		// Assert
		result
			.Should()
			.BeSameAs(keeper);
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindPasswordKeeper" />: returns null when a file belongs to no keeper.
	/// </summary>
	[Test]
	public void FindPasswordKeeper_Of_A_File_Without_A_Keeper_Returns_Null()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateNamedFolderDto("parent");

		FileDto file = ItemDtoFactory.CreateNamedFileDto("file");

		AddChild(parent, file);

		// Act
		FolderDto? result = file.FindPasswordKeeper();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FolderDto.FindPasswordKeeper" />: returns null when no keeper exists in the chain.
	/// </summary>
	[Test]
	public void FindPasswordKeeper_Returns_Null_When_No_Keeper_In_Chain()
	{
		// Arrange
		FolderDto parent = ItemDtoFactory.CreateNamedFolderDto("parent");

		FolderDto child = ItemDtoFactory.CreateNamedFolderDto("child");

		AddChild(parent, child);

		// Act
		FolderDto? result = child.FindPasswordKeeper();

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="FolderDto.FindPasswordKeeper" />: returns the nearest parent keeper when self is not one.
	/// </summary>
	[Test]
	public void FindPasswordKeeper_Returns_Parent_Keeper_When_Self_Is_Not()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateNamedFolderDto("keeper", encryptedDek: [1]);

		FolderDto child = ItemDtoFactory.CreateNamedFolderDto("child");

		AddChild(keeper, child);

		// Act
		FolderDto? result = child.FindPasswordKeeper();

		// Assert
		result
			.Should()
			.BeSameAs(keeper);
	}

	/// <summary>
	/// <see cref="FolderDto.FindPasswordKeeper" />: returns itself when self is a password keeper.
	/// </summary>
	[Test]
	public void FindPasswordKeeper_Returns_Self_When_Self_Is_Password_Keeper()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateNamedFolderDto("keeper", encryptedDek: [1]);

		// Act
		FolderDto? result = keeper.FindPasswordKeeper();

		// Assert
		result
			.Should()
			.BeSameAs(keeper);
	}

	/// <summary>
	/// <see cref="FolderDto.GetAllChildren" />: returns every descendant flattened into a single sequence.
	/// </summary>
	[Test]
	public void GetAllChildren_Returns_All_Descendants_Flattened()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateNamedFolderDto();

		FolderDto sub = ItemDtoFactory.CreateNamedFolderDto("sub");

		FileDto nested = ItemDtoFactory.CreateNamedFileDto("nested");

		FileDto top = ItemDtoFactory.CreateNamedFileDto("top");

		AddChild(root, sub);

		AddChild(sub, nested);

		AddChild(root, top);

		// Act
		List<ExplorerItemDtoBase> result = [.. root.GetAllChildren()];

		// Assert
		result
			.Should()
			.HaveCount(3);

		result
			.Should()
			.Contain([sub, nested, top]);
	}

	/// <summary>
	/// <see cref="FolderDto.IsPasswordKeeper" />: returns false when the encrypted DEK is empty.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_False_When_Dek_Empty()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateNamedFolderDto("folder", encryptedDek: []);

		// Act
		bool result = folder.IsPasswordKeeper();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderDto.IsPasswordKeeper" />: returns false when the encrypted DEK is missing.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_False_When_Dek_Missing()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateNamedFolderDto("folder");

		// Act
		bool result = folder.IsPasswordKeeper();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="FolderDto.IsPasswordKeeper" />: returns true when the encrypted DEK is present.
	/// </summary>
	[Test]
	public void IsPasswordKeeper_Returns_True_When_Dek_Present()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateNamedFolderDto("folder", encryptedDek: [1]);

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
	private static void AddChild(FolderDto parent, ExplorerItemDtoBase child)
	{
		child.Parent = parent;

		parent.Children.Add(child);
	}
	#endregion
}
