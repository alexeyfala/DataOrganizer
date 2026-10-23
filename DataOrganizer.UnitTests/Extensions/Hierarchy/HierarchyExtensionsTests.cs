using AwesomeAssertions;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Hierarchy;
using DataOrganizer.Extensions.Hierarchy;
using DataOrganizer.UnitTests.Factories;
using Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Extensions.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(HierarchyExtensions)}"" type")]
internal class HierarchyExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="HierarchyExtensions.AllBy" />: returns false when any item in the hierarchy fails the condition.
	/// </summary>
	[Test]
	public void AllBy_Returns_False_If_Any_Item_Does_Not_Satisfy_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto child = ItemDtoFactory.CreateFileDto(isEditing: true);

		root.Children.Add(child);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		bool result = hierarchy.AllBy(x => x is FolderDto);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.AllBy" />: a folder that fails the condition makes the answer false, though every
	/// file meets it.
	/// </summary>
	[Test]
	public void AllBy_Returns_False_When_A_Folder_Fails_The_Condition()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		ExplorerItemDtoBase[] hierarchy = [folder];

		// Act
		bool result = hierarchy.AllBy(x => x is FileDto);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.AllBy" />: returns true when every item in the hierarchy satisfies the condition.
	/// </summary>
	[Test]
	public void AllBy_Returns_True_When_All_Items_Match_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		root.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		bool result = hierarchy.AllBy(x => x is FolderDto);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.AllBy" />: the walk stops at the first object that fails the condition.
	/// </summary>
	[Test]
	public void AllBy_Stops_At_The_First_Object_That_Fails()
	{
		// Arrange
		List<ExplorerItemDtoBase> checkedItems = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		ExplorerItemDtoBase[] hierarchy = [folder, file];

		// Act
		hierarchy.AllBy(x =>
		{
			checkedItems.Add(x);

			return x is FolderDto;
		});

		// Assert
		checkedItems
			.Should()
			.Equal(file);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ContainsBy" />: returns false when no item in the hierarchy matches the predicate.
	/// </summary>
	[Test]
	public void ContainsBy_Generic_Predicate_Returns_False_When_No_Item_Matches()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		// Act
		bool result = hierarchy.ContainsBy(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ContainsBy" />: the walk stops at the first object that meets the condition.
	/// </summary>
	[Test]
	public void ContainsBy_Stops_At_The_First_Match()
	{
		// Arrange
		List<ExplorerItemDtoBase> checkedItems = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		ExplorerItemDtoBase[] hierarchy = [folder, file];

		// Act
		hierarchy.ContainsBy(x =>
		{
			checkedItems.Add(x);

			return x is FolderDto;
		});

		// Assert
		checkedItems
			.Should()
			.Equal(file, folder);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ContainsFileBy" />: returns true when a matching file exists deep in the hierarchy.
	/// </summary>
	[Test]
	public void ContainsFileBy_File_Predicate_Returns_True_When_Matching_File_Exists_Deep()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		FileDto target = ItemDtoFactory.CreateFileDto();

		nested.Children.Add(target);

		root.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		bool result = hierarchy.ContainsFileBy(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ContainsFileBy" />: the walk stops at the first file that meets the condition.
	/// </summary>
	[Test]
	public void ContainsFileBy_Stops_At_The_First_Matching_File()
	{
		// Arrange
		List<FileDto> checkedFiles = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(first);

		folder.Children.Add(second);

		ExplorerItemDtoBase[] hierarchy = [folder];

		// Act
		hierarchy.ContainsFileBy(x =>
		{
			checkedFiles.Add(x);

			return true;
		});

		// Assert
		checkedFiles
			.Should()
			.Equal(second);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ContainsId" />: returns false when no object with the given id is present.
	/// </summary>
	[Test]
	public void ContainsId_Returns_False_When_No_Object_With_Given_Id()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		// Act
		bool result = hierarchy.ContainsId(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ContainsId" />: returns true when the hierarchy contains an object with the given id.
	/// </summary>
	[Test]
	public void ContainsId_Returns_True_When_Hierarchy_Contains_Object_With_Given_Id()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto target = ItemDtoFactory.CreateFileDto();

		root.Children.Add(target);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		bool result = hierarchy.ContainsId(target.Id);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FilterBy" />: lists the objects in the order of the walk, the last top object first,
	/// each folder before its children and the last child first.
	/// </summary>
	[Test]
	public void FilterBy_Lists_The_Objects_In_The_Order_Of_The_Walk()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		FileDto folderFile = ItemDtoFactory.CreateFileDto();

		FileDto nestedFile = ItemDtoFactory.CreateFileDto();

		nested.Children.Add(nestedFile);

		folder.Children.Add(folderFile);

		folder.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [folder, file];

		// Act
		ExplorerItemDtoBase[] result = [.. hierarchy.FilterBy(_ => true)];

		// Assert
		result
			.Should()
			.Equal(file, folder, nested, nestedFile, folderFile);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FilterBy" />: returns a flat sequence of all items satisfying the condition.
	/// </summary>
	[Test]
	public void FilterBy_Returns_Flat_Sequence_Of_Items_Satisfying_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto file1 = ItemDtoFactory.CreateFileDto();

		FileDto file2 = ItemDtoFactory.CreateFileDto();

		root.Children.Add(file1);

		root.Children.Add(file2);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		ExplorerItemDtoBase[] result = [.. hierarchy.FilterBy(x => x is FileDto)];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.OnlyContain(x => x is FileDto);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FilterFilesById" />: returns only the files whose identifiers are in the given set.
	/// </summary>
	[Test]
	public void FilterFilesById_Returns_Files_With_Matching_Identifiers()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto[] files = [.. ItemDtoFactory.CreateFileDtos(5)];

		foreach (FileDto file in files)
		{
			root.Children.Add(file);
		}

		ExplorerItemDtoBase[] hierarchy = [root];

		Guid[] ids = [files[0].Id, files[2].Id, files[4].Id];

		// Act
		FileDto[] result = [.. hierarchy.FilterFilesById(ids)];

		// Assert
		result
			.Select(x => x.Id)
			.Should()
			.BeEquivalentTo(ids);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindBy" />: returns the matching item located in a nested folder.
	/// </summary>
	[Test]
	public void FindBy_Returns_Matching_Item_From_Nested_Folder()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		FileDto target = ItemDtoFactory.CreateFileDto();

		nested.Children.Add(target);

		root.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		ExplorerItemDtoBase? result = hierarchy.FindBy(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindBy" />: returns null when no item satisfies the condition.
	/// </summary>
	[Test]
	public void FindBy_Returns_Null_When_No_Item_Satisfies_Condition()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. ItemDtoFactory.CreateFolderDtos(3)];

		// Act
		ExplorerItemDtoBase? result = hierarchy.FindBy(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindBy" />: of several matches, returns the one the walk meets first.
	/// </summary>
	[Test]
	public void FindBy_Returns_The_Match_The_Walk_Meets_First()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(first);

		folder.Children.Add(second);

		ExplorerItemDtoBase[] hierarchy = [folder];

		// Act
		ExplorerItemDtoBase? result = hierarchy.FindBy(x => x is FileDto);

		// Assert
		result
			.Should()
			.Be(second);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindBy" />: the walk stops at the first object that meets the condition.
	/// </summary>
	[Test]
	public void FindBy_Stops_At_The_First_Match()
	{
		// Arrange
		List<ExplorerItemDtoBase> checkedItems = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(ItemDtoFactory.CreateFileDto());

		ExplorerItemDtoBase[] hierarchy = [folder, file];

		// Act
		hierarchy.FindBy(x =>
		{
			checkedItems.Add(x);

			return x is FolderDto;
		});

		// Assert
		checkedItems
			.Should()
			.Equal(file, folder);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindById" />: finds an item by its id via the underlying FindBy lookup.
	/// </summary>
	[Test]
	public void FindById_Delegates_To_FindBy()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto target = ItemDtoFactory.CreateFileDto();

		root.Children.Add(target);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		ExplorerItemDtoBase? result = hierarchy.FindById(target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindFileBy" />: returns the first file matching the condition.
	/// </summary>
	[Test]
	public void FindFileBy_Returns_First_File_Matching_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto(isEditing: true);

		root.Children.Add(file);

		root.Children.Add(ItemDtoFactory.CreateFolderDto());

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FileDto? result = hierarchy.FindFileBy(x => x.IsEditing);

		// Assert
		result
			.Should()
			.Be(file);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.FindFolderBy" />: returns the first folder matching the condition.
	/// </summary>
	[Test]
	public void FindFolderBy_Returns_First_Folder_Matching_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto target = ItemDtoFactory.CreateFolderDto();

		root.Children.Add(target);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FolderDto? result = hierarchy.FindFolderBy(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetCount(IEnumerable{ExplorerItemDtoBase})" />: a dataset counts as a file.
	/// </summary>
	[Test]
	public void GetCount_Counts_A_Dataset_As_A_File()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Children.Add(ItemDtoFactory.CreateFileDto(kind: EntityKind.Dataset));

		ExplorerItemDtoBase[] hierarchy = [folder];

		// Act
		HierarchyCounts result = hierarchy.GetCount();

		// Assert
		result.Files
			.Should()
			.Be(1);

		result.Folders
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetCount(IEnumerable{ExplorerItemDtoBase})" />: counts files and folders across the hierarchy separately.
	/// </summary>
	[Test]
	public void GetCount_For_Hierarchy_Counts_Files_And_Folders()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		root.Children.Add(ItemDtoFactory.CreateFileDto());

		root.Children.Add(ItemDtoFactory.CreateFileDto());

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		nested.Children.Add(ItemDtoFactory.CreateFileDto());

		root.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		HierarchyCounts result = hierarchy.GetCount();

		// Assert
		result.Files
			.Should()
			.Be(3);

		result.Folders
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetFiles" />: lists the files in the order of the walk.
	/// </summary>
	[Test]
	public void GetFiles_Lists_The_Files_In_The_Order_Of_The_Walk()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		FileDto folderFile = ItemDtoFactory.CreateFileDto();

		FileDto nestedFile = ItemDtoFactory.CreateFileDto();

		nested.Children.Add(nestedFile);

		folder.Children.Add(folderFile);

		folder.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [folder, file];

		// Act
		FileDto[] result = [.. hierarchy.GetFiles()];

		// Assert
		result
			.Should()
			.Equal(file, nestedFile, folderFile);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetFiles" />: returns files from all levels of the hierarchy.
	/// </summary>
	[Test]
	public void GetFiles_Returns_Files_From_All_Levels_Of_Hierarchy()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto rootFile = ItemDtoFactory.CreateFileDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		FileDto nestedFile = ItemDtoFactory.CreateFileDto();

		nested.Children.Add(nestedFile);

		root.Children.Add(rootFile);

		root.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FileDto[] result = [.. hierarchy.GetFiles()];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.Contain([rootFile, nestedFile]);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetFilesBy" />: returns only the files matching the condition.
	/// </summary>
	[Test]
	public void GetFilesBy_Returns_Files_Matching_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FileDto editing = ItemDtoFactory.CreateFileDto(isEditing: true);

		FileDto idle = ItemDtoFactory.CreateFileDto();

		root.Children.Add(editing);

		root.Children.Add(idle);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesBy(x => x.IsEditing)];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(editing);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetFolders" />: lists the folders in the order of the walk.
	/// </summary>
	[Test]
	public void GetFolders_Lists_The_Folders_In_The_Order_Of_The_Walk()
	{
		// Arrange
		FolderDto first = ItemDtoFactory.CreateFolderDto();

		FolderDto second = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		first.Children.Add(nested);

		ExplorerItemDtoBase[] hierarchy = [first, second];

		// Act
		FolderDto[] result = [.. hierarchy.GetFolders()];

		// Assert
		result
			.Should()
			.Equal(second, first, nested);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetFolders" />: returns all folders including nested ones.
	/// </summary>
	[Test]
	public void GetFolders_Returns_All_Folders_Including_Nested()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto nested = ItemDtoFactory.CreateFolderDto();

		root.Children.Add(nested);

		root.Children.Add(ItemDtoFactory.CreateFileDto());

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FolderDto[] result = [.. hierarchy.GetFolders()];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.Contain([root, nested]);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.GetFoldersBy" />: returns only the folders matching the condition.
	/// </summary>
	[Test]
	public void GetFoldersBy_Returns_Folders_Matching_Condition()
	{
		// Arrange
		FolderDto root = ItemDtoFactory.CreateFolderDto();

		FolderDto target = ItemDtoFactory.CreateFolderDto();

		root.Children.Add(target);

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FolderDto[] result = [.. hierarchy.GetFoldersBy(x => x.Id == target.Id)];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(target);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.SortByIndexRecursively" />: sorts top-level items and nested children by their index.
	/// </summary>
	[Test]
	public void SortByIndexRecursively_Sorts_Top_Level_And_Nested_Children_By_Index()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder.Index = 1;

		FileDto child1 = ItemDtoFactory.CreateFileDto();

		child1.Index = 30;

		FileDto child2 = ItemDtoFactory.CreateFileDto();

		child2.Index = 10;

		folder.Children.Add(child1);

		folder.Children.Add(child2);

		FileDto rootFile = ItemDtoFactory.CreateFileDto();

		rootFile.Index = 0;

		ExplorerItemDtoBase[] hierarchy = [folder, rootFile];

		// Act
		ExplorerItemDtoBase[] result = hierarchy.SortByIndexRecursively();

		// Assert
		result
			.Should()
			.Equal([rootFile, folder]);

		folder.Children
			.Should()
			.Equal([child2, child1]);
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.SortChildrenByIndexRecursively" />: returns without throwing for an empty folder.
	/// </summary>
	[Test]
	public void SortChildrenByIndexRecursively_Returns_Without_Action_For_Empty_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		// Act
		Action act = () => folder.SortChildrenByIndexRecursively();

		// Assert
		act
			.Should()
			.NotThrow();

		folder.Children
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.ToHierarchical" />: builds parent-child links and yields the root items.
	/// </summary>
	[Test]
	public void ToHierarchical_Builds_Parent_Child_Links_And_Yields_Roots()
	{
		// Arrange
		FolderDto rootFolder = ItemDtoFactory.CreateFolderDto();

		FolderDto childFolder = ItemDtoFactory.CreateFolderDto();

		childFolder.ParentId = rootFolder.Id;

		FileDto rootFile = ItemDtoFactory.CreateFileDto();

		FileDto childFile = ItemDtoFactory.CreateFileDto();

		childFile.ParentId = childFolder.Id;

		FolderDto[] folders = [rootFolder, childFolder];

		FileDto[] files = [rootFile, childFile];

		// Act
		ExplorerItemDtoBase[] result = [.. folders.ToHierarchical(files)];

		// Assert
		result
			.Should()
			.Contain([rootFolder, rootFile]);

		rootFolder.Children
			.Should()
			.Contain(childFolder);

		childFolder.Children
			.Should()
			.Contain(childFile);

		childFile.Parent
			.Should()
			.Be(childFolder);

		childFolder.Parent
			.Should()
			.Be(rootFolder);
	}
	#endregion
}
