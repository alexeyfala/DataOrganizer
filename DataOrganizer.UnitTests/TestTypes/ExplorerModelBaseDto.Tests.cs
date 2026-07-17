using AwesomeAssertions;
using DataOrganizer.DTO.Entities;
using Entities.Enums;
using System;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ExplorerModelBaseDto)}"" type")]
internal class ExplorerModelBaseDtoTests
{
	#region Methods
	/// <summary>
	/// <see cref="ExplorerModelBaseDto.AnyParent" />: returns false when no ancestor satisfies the condition.
	/// </summary>
	[Test]
	public void AnyParent_Returns_False_When_No_Parent_Matches()
	{
		// Arrange
		FolderModelDto parent = CreateFolder("parent");

		FileModelDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		bool result = child.AnyParent(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExplorerModelBaseDto.AnyParent" />: returns false when there is no parent at all.
	/// </summary>
	[Test]
	public void AnyParent_Returns_False_When_There_Is_No_Parent()
	{
		// Arrange
		FileModelDto orphan = CreateFile("orphan");

		// Act
		bool result = orphan.AnyParent(_ => true);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ExplorerModelBaseDto.AnyParent" />: returns true when an ancestor satisfies the condition.
	/// </summary>
	[Test]
	public void AnyParent_Returns_True_When_A_Parent_Matches()
	{
		// Arrange
		FolderModelDto grandparent = CreateFolder("grandparent");

		FolderModelDto parent = CreateFolder("parent");

		parent.Parent = grandparent;

		FileModelDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		bool result = child.AnyParent(x => x.Name == "grandparent");

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ExplorerModelBaseDto.FindParent" />: returns the nearest matching ancestor when walking up.
	/// </summary>
	[Test]
	public void FindParent_Returns_First_Matching_Parent_Walking_Up()
	{
		// Arrange
		FolderModelDto grandparent = CreateFolder("keep");

		FolderModelDto parent = CreateFolder("keep");

		parent.Parent = grandparent;

		FileModelDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		FolderModelDto? result = child.FindParent(x => x.Name == "keep");

		// Assert
		result
			.Should()
			.BeSameAs(parent);
	}

	/// <summary>
	/// <see cref="ExplorerModelBaseDto.FindParent" />: returns null when no ancestor satisfies the condition.
	/// </summary>
	[Test]
	public void FindParent_Returns_Null_When_No_Parent_Matches()
	{
		// Arrange
		FolderModelDto parent = CreateFolder("parent");

		FileModelDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		FolderModelDto? result = child.FindParent(x => x.Name == "missing");

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ExplorerModelBaseDto.GetAllParents" />: returns an empty sequence when there is no parent.
	/// </summary>
	[Test]
	public void GetAllParents_Returns_Empty_When_There_Is_No_Parent()
	{
		// Arrange
		FileModelDto orphan = CreateFile("orphan");

		// Act
		List<FolderModelDto> result = [.. orphan.GetAllParents()];

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ExplorerModelBaseDto.GetAllParents" />: returns the ancestors ordered from nearest to root.
	/// </summary>
	[Test]
	public void GetAllParents_Returns_Parents_From_Nearest_To_Root()
	{
		// Arrange
		FolderModelDto grandparent = CreateFolder("grandparent");

		FolderModelDto parent = CreateFolder("parent");

		parent.Parent = grandparent;

		FileModelDto child = CreateFile("child");

		child.Parent = parent;

		// Act
		List<FolderModelDto> result = [.. child.GetAllParents()];

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
	/// Creates a folder DTO with the required base members populated.
	/// </summary>
	private static FolderModelDto CreateFolder(string name = "") => new()
	{
		Id = Guid.NewGuid(),
		Index = 0,
		Name = name,
		CreatedDate = DateTime.UtcNow,
		UpdatedDate = DateTime.UtcNow,
		EntityType = EntityType.Folder
	};
	#endregion
}
