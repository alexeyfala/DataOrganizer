using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.Abstract;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using DataOrganizer.Extensions;
using DataOrganizer.Models;
using Repository.DTO;
using Shared.Common;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EnumerableExtensions)}"" type")]
internal class EnumerableExtensionsTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="EnumerableExtensions.AllBy" />.
	/// </summary>
	[Test]
	public void AllBy_Returns_False_If_Any_Item_Does_Not_Satisfy_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto child = TestUtils.CreateFileDto(isEditing: true);

		root.Children.Add(child);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		bool result = hierarchy.AllBy(x => x is FolderModelDto);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.AllBy" />.
	/// </summary>
	[Test]
	public void AllBy_Returns_True_When_All_Items_Match_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FolderModelDto nested = TestUtils.CreateFolderDto();

		root.Children.Add(nested);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		bool result = hierarchy.AllBy(x => x is FolderModelDto);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.ContainsBy(IEnumerable{ExplorerModelBaseDto}, Predicate{FileModelDto})" />.
	/// </summary>
	[Test]
	public void ContainsBy_File_Predicate_Returns_True_When_Matching_File_Exists_Deep()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FolderModelDto nested = TestUtils.CreateFolderDto();

		FileModelDto target = TestUtils.CreateFileDto();

		nested.Children.Add(target);

		root.Children.Add(nested);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		bool result = hierarchy.ContainsBy((Predicate<FileModelDto>)(x => x.Id == target.Id));

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.ContainsBy(IEnumerable{ExplorerModelBaseDto}, Predicate{ExplorerModelBaseDto})" />.
	/// </summary>
	[Test]
	public void ContainsBy_Generic_Predicate_Returns_False_When_No_Item_Matches()
	{
		// Arrange
		ExplorerModelBaseDto[] hierarchy = [.. TestUtils.CreateFoldersDto(3)];

		// Act
		bool result = hierarchy.ContainsBy((Predicate<ExplorerModelBaseDto>)(x => x.Id == Guid.NewGuid()));

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.ContainsId" />.
	/// </summary>
	[Test]
	public void ContainsId_Returns_False_When_No_Object_With_Given_Id()
	{
		// Arrange
		ExplorerModelBaseDto[] hierarchy = [.. TestUtils.CreateFoldersDto(3)];

		// Act
		bool result = hierarchy.ContainsId(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.ContainsId" />.
	/// </summary>
	[Test]
	public void ContainsId_Returns_True_When_Hierarchy_Contains_Object_With_Given_Id()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto target = TestUtils.CreateFileDto();

		root.Children.Add(target);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		bool result = hierarchy.ContainsId(target.Id);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FilterBy" />.
	/// </summary>
	[Test]
	public void FilterBy_Returns_Flat_Sequence_Of_Items_Satisfying_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto file1 = TestUtils.CreateFileDto();

		FileModelDto file2 = TestUtils.CreateFileDto();

		root.Children.Add(file1);

		root.Children.Add(file2);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		ExplorerModelBaseDto[] result = [.. hierarchy.FilterBy(x => x is FileModelDto)];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.OnlyContain(x => x is FileModelDto);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FilterFilesById" />.
	/// </summary>
	[Test]
	public void FilterFilesById_Returns_Files_With_Matching_Identifiers()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto[] files = [.. TestUtils.CreateFilesDto(5)];

		foreach (FileModelDto file in files)
		{
			root.Children.Add(file);
		}

		ExplorerModelBaseDto[] hierarchy = [root];

		Guid[] ids = [files[0].Id, files[2].Id, files[4].Id];

		// Act
		FileModelDto[] result = [.. hierarchy.FilterFilesById(ids)];

		// Assert
		result
			.Select(x => x.Id)
			.Should()
			.BeEquivalentTo(ids);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FindBy" />.
	/// </summary>
	[Test]
	public void FindBy_Returns_Matching_Item_From_Nested_Folder()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FolderModelDto nested = TestUtils.CreateFolderDto();

		FileModelDto target = TestUtils.CreateFileDto();

		nested.Children.Add(target);

		root.Children.Add(nested);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		ExplorerModelBaseDto? result = hierarchy.FindBy(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FindBy" />.
	/// </summary>
	[Test]
	public void FindBy_Returns_Null_When_No_Item_Satisfies_Condition()
	{
		// Arrange
		ExplorerModelBaseDto[] hierarchy = [.. TestUtils.CreateFoldersDto(3)];

		// Act
		ExplorerModelBaseDto? result = hierarchy.FindBy(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FindById" />.
	/// </summary>
	[Test]
	public void FindById_Delegates_To_FindBy()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto target = TestUtils.CreateFileDto();

		root.Children.Add(target);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		ExplorerModelBaseDto? result = hierarchy.FindById(target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FindFileBy" />.
	/// </summary>
	[Test]
	public void FindFileBy_Returns_First_File_Matching_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto file = TestUtils.CreateFileDto(isEditing: true);

		root.Children.Add(file);

		root.Children.Add(TestUtils.CreateFolderDto());

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FileModelDto? result = hierarchy.FindFileBy(x => x.IsEditing);

		// Assert
		result
			.Should()
			.Be(file);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.FindFolderBy" />.
	/// </summary>
	[Test]
	public void FindFolderBy_Returns_First_Folder_Matching_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FolderModelDto target = TestUtils.CreateFolderDto();

		root.Children.Add(target);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FolderModelDto? result = hierarchy.FindFolderBy(x => x.Id == target.Id);

		// Assert
		result
			.Should()
			.Be(target);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.Flatten" />.
	/// </summary>
	[Test]
	public void Flatten_Returns_Every_Record_Including_Nested_Group_Children()
	{
		// Arrange
		ValueRecord leaf1 = new() { Value = AppUtils.CreateRandomString(5) };

		ValueRecord leaf2 = new() { Value = AppUtils.CreateRandomString(5) };

		RecordsGroup nested = new() { Name = AppUtils.CreateRandomString(5) };

		nested.Children.Add(leaf2);

		RecordsGroup root = new() { Name = AppUtils.CreateRandomString(5) };

		root.Children.Add(leaf1);

		root.Children.Add(nested);

		DatasetRecordBase[] hierarchy = [root];

		// Act
		DatasetRecordBase[] result = [.. hierarchy.Flatten()];

		// Assert
		result
			.Should()
			.HaveCount(4);

		result
			.Should()
			.Contain([root, nested, leaf1, leaf2]);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetCount(IEnumerable{DatasetRecordBase})" />.
	/// </summary>
	[Test]
	public void GetCount_For_DatasetRecord_Returns_Total_Count_Including_Children()
	{
		// Arrange
		RecordsGroup group = new() { Name = AppUtils.CreateRandomString(5) };

		group.Children.Add(new ValueRecord { Value = AppUtils.CreateRandomString(5) });

		group.Children.Add(new ValueRecord { Value = AppUtils.CreateRandomString(5) });

		DatasetRecordBase[] hierarchy = [group];

		// Act
		int result = hierarchy.GetCount();

		// Assert
		result
			.Should()
			.Be(3);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetCount(IEnumerable{ExplorerModelBaseDto})" /> (internal).
	/// </summary>
	[Test]
	public void GetCount_For_Hierarchy_Counts_Files_And_Folders()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		root.Children.Add(TestUtils.CreateFileDto());

		root.Children.Add(TestUtils.CreateFileDto());

		FolderModelDto nested = TestUtils.CreateFolderDto();

		nested.Children.Add(TestUtils.CreateFileDto());

		root.Children.Add(nested);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FilesFoldersNumberPair result = hierarchy.GetCount();

		// Assert
		result.Files
			.Should()
			.Be(3);

		result.Folders
			.Should()
			.Be(2);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetFiles" />.
	/// </summary>
	[Test]
	public void GetFiles_Returns_Files_From_All_Levels_Of_Hierarchy()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto rootFile = TestUtils.CreateFileDto();

		FolderModelDto nested = TestUtils.CreateFolderDto();

		FileModelDto nestedFile = TestUtils.CreateFileDto();

		nested.Children.Add(nestedFile);

		root.Children.Add(rootFile);

		root.Children.Add(nested);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FileModelDto[] result = [.. hierarchy.GetFiles()];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.Contain([rootFile, nestedFile]);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetFilesBy" />.
	/// </summary>
	[Test]
	public void GetFilesBy_Returns_Files_Matching_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FileModelDto editing = TestUtils.CreateFileDto(isEditing: true);

		FileModelDto idle = TestUtils.CreateFileDto();

		root.Children.Add(editing);

		root.Children.Add(idle);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FileModelDto[] result = [.. hierarchy.GetFilesBy(x => x.IsEditing)];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(editing);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetFolders" />.
	/// </summary>
	[Test]
	public void GetFolders_Returns_All_Folders_Including_Nested()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FolderModelDto nested = TestUtils.CreateFolderDto();

		root.Children.Add(nested);

		root.Children.Add(TestUtils.CreateFileDto());

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FolderModelDto[] result = [.. hierarchy.GetFolders()];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result
			.Should()
			.Contain([root, nested]);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetFoldersBy" />.
	/// </summary>
	[Test]
	public void GetFoldersBy_Returns_Folders_Matching_Condition()
	{
		// Arrange
		FolderModelDto root = TestUtils.CreateFolderDto();

		FolderModelDto target = TestUtils.CreateFolderDto();

		root.Children.Add(target);

		ExplorerModelBaseDto[] hierarchy = [root];

		// Act
		FolderModelDto[] result = [.. hierarchy.GetFoldersBy(x => x.Id == target.Id)];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(target);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetHotkeysPresentation" />.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Includes_Mask_When_Not_Default()
	{
		// Arrange
		CodeMaskPair[] pairs =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.LeftCtrl },
			new() { Code = KeyCode.VcB, Mask = EventMask.None }
		];

		// Act
		string result = pairs.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.StartWith("LeftCtrl + ");

		result
			.Should()
			.Contain("A, B");
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetHotkeysPresentation" />.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Returns_Empty_String_For_Empty_Sequence()
	{
		// Arrange
		CodeMaskPair[] pairs = [];

		// Act
		string result = pairs.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.GetHotkeysPresentation" />.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Skips_Mask_When_Default()
	{
		// Arrange
		CodeMaskPair[] pairs =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.None },
			new() { Code = KeyCode.VcB, Mask = EventMask.None }
		];

		// Act
		string result = pairs.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.Be("A, B");
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.Sort" />.
	/// </summary>
	[Test]
	public void Sort_Sorts_Groups_KeyValues_And_Values_In_Specified_Direction([Values] ListSortDirection direction)
	{
		// Arrange
		RecordsGroup groupA = new() { Name = "A" };

		RecordsGroup groupB = new() { Name = "B" };

		KeyValueRecord kvX = new() { Key = "X", Value = "1" };

		KeyValueRecord kvY = new() { Key = "Y", Value = "2" };

		ValueRecord vP = new() { Value = "P" };

		ValueRecord vQ = new() { Value = "Q" };

		DatasetRecordBase[] records = [groupB, kvY, vQ, kvX, vP, groupA];

		// Act
		DatasetRecordBase[] result = records.Sort(direction);

		// Assert
		RecordsGroup[] groups = [.. result.OfType<RecordsGroup>()];

		KeyValueRecord[] kvs = [.. result.OfType<KeyValueRecord>()];

		ValueRecord[] values = [.. result.Where(x => x.GetType() == typeof(ValueRecord)).Cast<ValueRecord>()];

		if (direction == ListSortDirection.Ascending)
		{
			groups
				.Select(x => x.Name)
				.Should()
				.Equal("A", "B");

			kvs
				.Select(x => x.Key)
				.Should()
				.Equal("X", "Y");

			values
				.Select(x => x.Value)
				.Should()
				.Equal("P", "Q");
		}
		else
		{
			groups
				.Select(x => x.Name)
				.Should()
				.Equal("B", "A");

			kvs
				.Select(x => x.Key)
				.Should()
				.Equal("Y", "X");

			values
				.Select(x => x.Value)
				.Should()
				.Equal("Q", "P");
		}
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.SortByIndexRecursively" />.
	/// </summary>
	[Test]
	public void SortByIndexRecursively_Sorts_Top_Level_And_Nested_Children_By_Index()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

		folder.Index = 1;

		FileModelDto child1 = TestUtils.CreateFileDto();

		child1.Index = 30;

		FileModelDto child2 = TestUtils.CreateFileDto();

		child2.Index = 10;

		folder.Children.Add(child1);

		folder.Children.Add(child2);

		FileModelDto rootFile = TestUtils.CreateFileDto();

		rootFile.Index = 0;

		ExplorerModelBaseDto[] hierarchy = [folder, rootFile];

		// Act
		ExplorerModelBaseDto[] result = hierarchy.SortByIndexRecursively();

		// Assert
		result
			.Should()
			.Equal([rootFile, folder]);

		folder.Children
			.Should()
			.Equal([child2, child1]);
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.SortChildrenByIndexRecursively" />.
	/// </summary>
	[Test]
	public void SortChildrenByIndexRecursively_Returns_Without_Action_For_Empty_Folder()
	{
		// Arrange
		FolderModelDto folder = TestUtils.CreateFolderDto();

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
	/// Test of <see cref="EnumerableExtensions.ToCodeMaskPairs" />.
	/// </summary>
	[Test]
	public void ToCodeMaskPairs_Maps_HotkeyModelDto_To_CodeMaskPair()
	{
		// Arrange
		HotkeyModelDto[] hotkeys = [.. TestUtils.CreateHotkeysDto(3)];

		// Act
		CodeMaskPair[] result = [.. hotkeys.ToCodeMaskPairs()];

		// Assert
		result
			.Should()
			.HaveCount(3);

		for (int i = 0; i < hotkeys.Length; i++)
		{
			result[i].Code
				.Should()
				.Be(hotkeys[i].Code);

			result[i].Mask
				.Should()
				.Be(hotkeys[i].Mask);
		}
	}

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.ToHierarchical" />.
	/// </summary>
	[Test]
	public void ToHierarchical_Builds_Parent_Child_Links_And_Yields_Roots()
	{
		// Arrange
		FolderModelDto rootFolder = TestUtils.CreateFolderDto();

		FolderModelDto childFolder = TestUtils.CreateFolderDto();

		childFolder.ParentId = rootFolder.Id;

		FileModelDto rootFile = TestUtils.CreateFileDto();

		FileModelDto childFile = TestUtils.CreateFileDto();

		childFile.ParentId = childFolder.Id;

		FolderModelDto[] folders = [rootFolder, childFolder];

		FileModelDto[] files = [rootFile, childFile];

		// Act
		ExplorerModelBaseDto[] result = [.. folders.ToHierarchical(files)];

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

	/// <summary>
	/// Test of <see cref="EnumerableExtensions.ToHotkeyModelsDto" />.
	/// </summary>
	[Test]
	public void ToHotkeyModelsDto_Maps_Pairs_With_Sequential_Indexes()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		Guid ownerId = Guid.NewGuid();

		CodeMaskPair[] pairs =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.LeftCtrl },
			new() { Code = KeyCode.VcB, Mask = EventMask.LeftShift }
		];

		// Act
		HotkeyModelDto[] result = [.. pairs.ToHotkeyModelsDto(id, ownerId)];

		// Assert
		result
			.Should()
			.HaveCount(2);

		result[0].Index
			.Should()
			.Be(0);

		result[1].Index
			.Should()
			.Be(1);

		result
			.Should()
			.OnlyContain(x => x.Id == id && x.OwnerId == ownerId);

		result[0].Code
			.Should()
			.Be(KeyCode.VcA);

		result[1].Mask
			.Should()
			.Be(EventMask.LeftShift);
	}
	#endregion
}
