using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using Entities.Enums;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Dto.Entities;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerItemDtoBase)}"" type")]
internal class ExplorerItemDtoBaseTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerItemDtoBase.AnyParent" />: returns false when no ancestor satisfies the condition.
	/// </summary>
	[Test]
	public void AnyParent_Returns_False_When_No_Parent_Matches()
	{
		// Arrange
		FolderDto parent = CreateFolder("parent");

		FileDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		bool result = child.AnyParent(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.AnyParent" />: returns false when there is no parent at all.
	/// </summary>
	[Test]
	public void AnyParent_Returns_False_When_There_Is_No_Parent()
	{
		// Arrange
		FileDto orphan = CreateFile("orphan");

		// Act
		bool result = orphan.AnyParent(_ => true);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.AnyParent" />: returns true when an ancestor satisfies the condition.
	/// </summary>
	[Test]
	public void AnyParent_Returns_True_When_A_Parent_Matches()
	{
		// Arrange
		FolderDto grandparent = CreateFolder("grandparent");

		FolderDto parent = CreateFolder("parent");

		parent.Parent = grandparent;

		FileDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		bool result = child.AnyParent(x => x.Name == "grandparent");

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.FindParent" />: returns the nearest matching ancestor when walking up.
	/// </summary>
	[Test]
	public void FindParent_Returns_First_Matching_Parent_Walking_Up()
	{
		// Arrange
		FolderDto grandparent = CreateFolder("keep");

		FolderDto parent = CreateFolder("keep");

		parent.Parent = grandparent;

		FileDto child = CreateFile("child");

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
		FolderDto parent = CreateFolder("parent");

		FileDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		FolderDto? result = child.FindParent(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ExplorerItemDtoBase.GetAllParents" />: returns an empty sequence when there is no parent.
	/// </summary>
	[Test]
	public void GetAllParents_Returns_Empty_When_There_Is_No_Parent()
	{
		// Arrange
		FileDto orphan = CreateFile("orphan");

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
		FolderDto grandparent = CreateFolder("grandparent");

		FolderDto parent = CreateFolder("parent");

		parent.Parent = grandparent;

		FileDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		List<FolderDto> result = [.. child.GetAllParents()];

		// Assert
		result
			.Should()
			.Equal(parent, grandparent);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a file DTO with the required base members populated.
	/// </summary>
	private static FileDto CreateFile(string name = "") => new()
	{
		Id = Guid.NewGuid(),
		Index = 0,
		Name = name,
		CreatedAt = DateTime.UtcNow,
		UpdatedAt = DateTime.UtcNow,
		Kind = EntityKind.File
	};

	/// <summary>
	/// Creates a folder DTO with the required base members populated.
	/// </summary>
	private static FolderDto CreateFolder(string name = "") => new()
	{
		Id = Guid.NewGuid(),
		Index = 0,
		Name = name,
		CreatedAt = DateTime.UtcNow,
		UpdatedAt = DateTime.UtcNow,
		Kind = EntityKind.Folder
	};
	#endregion
}
