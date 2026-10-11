using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.UnitTests.Factories;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataOrganizer.UnitTests.Extensions;

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
	/// <see cref="HierarchyExtensions.MergeManyInTree" />: an object added to a folder of the tree is followed from then on.
	/// </summary>
	[Test]
	public void MergeManyInTree_Follows_An_Object_Added_Later()
	{
		// Arrange
		List<string> renamed = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [folder];

		using IDisposable subscription = hierarchy
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.Name, notifyOnInitialValue: false))
			.Subscribe(x => renamed.Add(x.Sender.Name));

		folder.Children.Add(file);

		// Act
		file.Name = "added";

		// Assert
		renamed
			.Should()
			.Equal("added");
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.MergeManyInTree" />: an object removed from the tree is no longer followed.
	/// </summary>
	[Test]
	public void MergeManyInTree_Lets_Go_Of_An_Object_Removed()
	{
		// Arrange
		List<string> renamed = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		folder.Children.Add(file);

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [folder];

		using IDisposable subscription = hierarchy
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.Name, notifyOnInitialValue: false))
			.Subscribe(x => renamed.Add(x.Sender.Name));

		folder.Children.Remove(file);

		// Act
		file.Name = "removed";

		// Assert
		renamed
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HierarchyExtensions.MergeManyInTree" />: the objects of the tree are followed at every depth, the folders
	/// among them.
	/// </summary>
	[Test]
	public void MergeManyInTree_Merges_The_Objects_At_Any_Depth()
	{
		// Arrange
		List<string> renamed = [];

		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		FolderDto innerFolder = ItemDtoFactory.CreateFolderDto();

		FileDto innerFile = ItemDtoFactory.CreateFileDto();

		FileDto file = ItemDtoFactory.CreateFileDto();

		innerFolder.Children.Add(innerFile);

		folder.Children.Add(innerFolder);

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [folder, file];

		using IDisposable subscription = hierarchy
			.MergeManyInTree(x => x.WhenPropertyChanged(y => y.Name, notifyOnInitialValue: false))
			.Subscribe(x => renamed.Add(x.Sender.Name));

		// Act
		folder.Name = "folder";

		innerFolder.Name = "inner folder";

		innerFile.Name = "inner file";

		file.Name = "file";

		// Assert
		renamed
			.Should()
			.Equal("folder", "inner folder", "inner file", "file");
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
