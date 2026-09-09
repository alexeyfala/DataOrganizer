using AwesomeAssertions;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Extensions;
using DataOrganizer.Models.Dataset;
using Repository.Dto;
using Shared.Common;
using Shared.Properties;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using TestSupport;

namespace DataOrganizer.UnitTests;

[TestFixture(Description = $@"Tests of ""{nameof(EnumerableExtensions)}"" type")]
internal class EnumerableExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="EnumerableExtensions.AllBy" />: returns false when any item in the hierarchy fails the condition.
	/// </summary>
	[Test]
	public void AllBy_Returns_False_If_Any_Item_Does_Not_Satisfy_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto child = TestData.CreateFileDto(isEditing: true);

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
	/// <see cref="EnumerableExtensions.AllBy" />: returns true when every item in the hierarchy satisfies the condition.
	/// </summary>
	[Test]
	public void AllBy_Returns_True_When_All_Items_Match_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FolderDto nested = TestData.CreateFolderDto();

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
	/// <see cref="EnumerableExtensions.ContainsBy" />: returns false when no item in the hierarchy matches the predicate.
	/// </summary>
	[Test]
	public void ContainsBy_Generic_Predicate_Returns_False_When_No_Item_Matches()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. TestData.CreateFoldersDto(3)];

		// Act
		bool result = hierarchy.ContainsBy(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.ContainsFileBy" />: returns true when a matching file exists deep in the hierarchy.
	/// </summary>
	[Test]
	public void ContainsFileBy_File_Predicate_Returns_True_When_Matching_File_Exists_Deep()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FolderDto nested = TestData.CreateFolderDto();

		FileDto target = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.ContainsId" />: returns false when no object with the given id is present.
	/// </summary>
	[Test]
	public void ContainsId_Returns_False_When_No_Object_With_Given_Id()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. TestData.CreateFoldersDto(3)];

		// Act
		bool result = hierarchy.ContainsId(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.ContainsId" />: returns true when the hierarchy contains an object with the given id.
	/// </summary>
	[Test]
	public void ContainsId_Returns_True_When_Hierarchy_Contains_Object_With_Given_Id()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto target = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.FilterBy" />: returns a flat sequence of all items satisfying the condition.
	/// </summary>
	[Test]
	public void FilterBy_Returns_Flat_Sequence_Of_Items_Satisfying_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto file1 = TestData.CreateFileDto();

		FileDto file2 = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.FilterFilesById" />: returns only the files whose identifiers are in the given set.
	/// </summary>
	[Test]
	public void FilterFilesById_Returns_Files_With_Matching_Identifiers()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto[] files = [.. TestData.CreateFilesDto(5)];

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
	/// <see cref="EnumerableExtensions.FindBy" />: returns the matching item located in a nested folder.
	/// </summary>
	[Test]
	public void FindBy_Returns_Matching_Item_From_Nested_Folder()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FolderDto nested = TestData.CreateFolderDto();

		FileDto target = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.FindBy" />: returns null when no item satisfies the condition.
	/// </summary>
	[Test]
	public void FindBy_Returns_Null_When_No_Item_Satisfies_Condition()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [.. TestData.CreateFoldersDto(3)];

		// Act
		ExplorerItemDtoBase? result = hierarchy.FindBy(x => x.Id == Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.FindById" />: finds an item by its id via the underlying FindBy lookup.
	/// </summary>
	[Test]
	public void FindById_Delegates_To_FindBy()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto target = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.FindFileBy" />: returns the first file matching the condition.
	/// </summary>
	[Test]
	public void FindFileBy_Returns_First_File_Matching_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto file = TestData.CreateFileDto(isEditing: true);

		root.Children.Add(file);

		root.Children.Add(TestData.CreateFolderDto());

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FileDto? result = hierarchy.FindFileBy(x => x.IsEditing);

		// Assert
		result
			.Should()
			.Be(file);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.FindFolderBy" />: returns the first folder matching the condition.
	/// </summary>
	[Test]
	public void FindFolderBy_Returns_First_Folder_Matching_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FolderDto target = TestData.CreateFolderDto();

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
	/// <see cref="EnumerableExtensions.Flatten" />: returns every record including nested group children.
	/// </summary>
	[Test]
	public void Flatten_Returns_Every_Record_Including_Nested_Group_Children()
	{
		// Arrange
		ValueRecord leaf1 = new() { Value = RandomString.Create(5) };

		ValueRecord leaf2 = new() { Value = RandomString.Create(5) };

		RecordsGroup nested = new() { Name = RandomString.Create(5) };

		nested.Children.Add(leaf2);

		RecordsGroup root = new() { Name = RandomString.Create(5) };

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
	/// <see cref="EnumerableExtensions.GetCount(IEnumerable{DatasetRecordBase})" />: returns the total record count including children.
	/// </summary>
	[Test]
	public void GetCount_For_DatasetRecord_Returns_Total_Count_Including_Children()
	{
		// Arrange
		RecordsGroup group = new() { Name = RandomString.Create(5) };

		group.Children.Add(new ValueRecord { Value = RandomString.Create(5) });

		group.Children.Add(new ValueRecord { Value = RandomString.Create(5) });

		DatasetRecordBase[] hierarchy = [group];

		// Act
		int result = hierarchy.GetCount();

		// Assert
		result
			.Should()
			.Be(3);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetCount(IEnumerable{ExplorerItemDtoBase})" />: counts files and folders across the hierarchy separately.
	/// </summary>
	[Test]
	public void GetCount_For_Hierarchy_Counts_Files_And_Folders()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		root.Children.Add(TestData.CreateFileDto());

		root.Children.Add(TestData.CreateFileDto());

		FolderDto nested = TestData.CreateFolderDto();

		nested.Children.Add(TestData.CreateFileDto());

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
	/// <see cref="EnumerableExtensions.GetFiles" />: returns files from all levels of the hierarchy.
	/// </summary>
	[Test]
	public void GetFiles_Returns_Files_From_All_Levels_Of_Hierarchy()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto rootFile = TestData.CreateFileDto();

		FolderDto nested = TestData.CreateFolderDto();

		FileDto nestedFile = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.GetFilesBy" />: returns only the files matching the condition.
	/// </summary>
	[Test]
	public void GetFilesBy_Returns_Files_Matching_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto editing = TestData.CreateFileDto(isEditing: true);

		FileDto idle = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.GetFilesWithUnreadableHotkeys" />: returns a file whose hotkey holds an unreadable key.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Returns_File_With_Unreadable_Key()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FileDto damaged = CreateFileWithHotkey(KeyCode.VcUndefined, EventMask.LeftCtrl);

		root.Children.Add(damaged);

		root.Children.Add(CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl));

		ExplorerItemDtoBase[] hierarchy = [root];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(damaged);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetFilesWithUnreadableHotkeys" />: returns a file whose hotkey holds an unreadable mask.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Returns_File_With_Unreadable_Mask()
	{
		// Arrange
		FileDto damaged = CreateFileWithHotkey(KeyCode.VcA, EventMask.None);

		ExplorerItemDtoBase[] hierarchy = [damaged];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(damaged);
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetFilesWithUnreadableHotkeys" />: skips a file that has no hotkeys.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Skips_File_Without_Hotkeys()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy = [TestData.CreateFileDto()];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetFilesWithUnreadableHotkeys" />: skips a file whose hotkeys are readable.
	/// </summary>
	[Test]
	public void GetFilesWithUnreadableHotkeys_Skips_Readable_Hotkeys()
	{
		// Arrange
		ExplorerItemDtoBase[] hierarchy =
		[
			CreateFileWithHotkey(KeyCode.VcA, EventMask.LeftCtrl),
			CreateFileWithHotkey(KeyCode.VcB, EventMask.LeftShift | EventMask.LeftAlt)
		];

		// Act
		FileDto[] result = [.. hierarchy.GetFilesWithUnreadableHotkeys()];

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetFolders" />: returns all folders including nested ones.
	/// </summary>
	[Test]
	public void GetFolders_Returns_All_Folders_Including_Nested()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FolderDto nested = TestData.CreateFolderDto();

		root.Children.Add(nested);

		root.Children.Add(TestData.CreateFileDto());

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
	/// <see cref="EnumerableExtensions.GetFoldersBy" />: returns only the folders matching the condition.
	/// </summary>
	[Test]
	public void GetFoldersBy_Returns_Folders_Matching_Condition()
	{
		// Arrange
		FolderDto root = TestData.CreateFolderDto();

		FolderDto target = TestData.CreateFolderDto();

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
	/// <see cref="EnumerableExtensions.GetHotkeysPresentation" />: includes the mask prefix when it is not the default value.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Includes_Mask_When_Not_Default()
	{
		// Arrange
		KeyStroke[] keyStrokes =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.LeftCtrl },
			new() { Code = KeyCode.VcB, Mask = EventMask.None }
		];

		// Act
		string result = keyStrokes.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.StartWith("LeftCtrl + ");

		result
			.Should()
			.Contain("A, B");
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetHotkeysPresentation" />: returns an empty string for an empty sequence.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Returns_Empty_String_For_Empty_Sequence()
	{
		// Arrange
		KeyStroke[] keyStrokes = [];

		// Act
		string result = keyStrokes.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetHotkeysPresentation" />: omits the mask prefix when it is the default value.
	/// </summary>
	[Test]
	public void GetHotkeysPresentation_Skips_Mask_When_Default()
	{
		// Arrange
		KeyStroke[] keyStrokes =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.None },
			new() { Code = KeyCode.VcB, Mask = EventMask.None }
		];

		// Act
		string result = keyStrokes.GetHotkeysPresentation();

		// Assert
		result
			.Should()
			.Be("A, B");
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetUnreadableHotkeysPresentation" />: counts the files left out when there are more than three.
	/// </summary>
	[Test]
	public void GetUnreadableHotkeysPresentation_Counts_The_Rest()
	{
		// Arrange
		FileDto[] files = [.. TestData.CreateFilesDto(5)];

		// Act
		string result = files.GetUnreadableHotkeysPresentation(Strings.FailedToReadHotkeys);

		// Assert
		result
			.Should()
			.Contain($@"""{files[2].Name}""");

		result
			.Should()
			.NotContain($@"""{files[3].Name}""");

		result
			.Should()
			.EndWith(string.Format(CultureInfo.CurrentCulture, Strings.AndMore, 2));
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.GetUnreadableHotkeysPresentation" />: names every file when there are no more than three.
	/// </summary>
	[Test]
	public void GetUnreadableHotkeysPresentation_Names_Every_File()
	{
		// Arrange
		FileDto[] files = [.. TestData.CreateFilesDto(3)];

		// Act
		string result = files.GetUnreadableHotkeysPresentation(Strings.FailedToReadHotkeys);

		// Assert
		result
			.Should()
			.StartWith(Strings.FailedToReadHotkeys);

		foreach (FileDto file in files)
		{
			result
				.Should()
				.Contain($@"{Environment.NewLine}""{file.Name}""");
		}
	}

	/// <summary>
	/// <see cref="EnumerableExtensions.Sort" />: sorts groups, key-value records, and values in the specified direction.
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
	/// <see cref="EnumerableExtensions.SortByIndexRecursively" />: sorts top-level items and nested children by their index.
	/// </summary>
	[Test]
	public void SortByIndexRecursively_Sorts_Top_Level_And_Nested_Children_By_Index()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto();

		folder.Index = 1;

		FileDto child1 = TestData.CreateFileDto();

		child1.Index = 30;

		FileDto child2 = TestData.CreateFileDto();

		child2.Index = 10;

		folder.Children.Add(child1);

		folder.Children.Add(child2);

		FileDto rootFile = TestData.CreateFileDto();

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
	/// <see cref="EnumerableExtensions.SortChildrenByIndexRecursively" />: returns without throwing for an empty folder.
	/// </summary>
	[Test]
	public void SortChildrenByIndexRecursively_Returns_Without_Action_For_Empty_Folder()
	{
		// Arrange
		FolderDto folder = TestData.CreateFolderDto();

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
	/// <see cref="EnumerableExtensions.ToKeyStrokes" />: maps each hotkey to a key stroke preserving code and mask.
	/// </summary>
	[Test]
	public void ToKeyStrokes_Maps_HotkeyDto_To_KeyStroke()
	{
		// Arrange
		HotkeyDto[] hotkeys = [.. TestData.CreateHotkeysDto(3)];

		// Act
		KeyStroke[] result = [.. hotkeys.ToKeyStrokes()];

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
	/// <see cref="EnumerableExtensions.ToHierarchical" />: builds parent-child links and yields the root items.
	/// </summary>
	[Test]
	public void ToHierarchical_Builds_Parent_Child_Links_And_Yields_Roots()
	{
		// Arrange
		FolderDto rootFolder = TestData.CreateFolderDto();

		FolderDto childFolder = TestData.CreateFolderDto();

		childFolder.ParentId = rootFolder.Id;

		FileDto rootFile = TestData.CreateFileDto();

		FileDto childFile = TestData.CreateFileDto();

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

	/// <summary>
	/// <see cref="EnumerableExtensions.ToHotkeyDtos" />: maps keyStrokes to hotkey models with sequential indexes and shared ids.
	/// </summary>
	[Test]
	public void ToHotkeyDtos_Maps_Pairs_With_Sequential_Indexes()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		Guid ownerId = Guid.NewGuid();

		KeyStroke[] keyStrokes =
		[
			new() { Code = KeyCode.VcA, Mask = EventMask.LeftCtrl },
			new() { Code = KeyCode.VcB, Mask = EventMask.LeftShift }
		];

		// Act
		HotkeyDto[] result = [.. keyStrokes.ToHotkeyDtos(id, ownerId)];

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

	#region Helpers
	/// <summary>
	/// Creates a file that owns a single hotkey.
	/// </summary>
	private static FileDto CreateFileWithHotkey(KeyCode code, EventMask mask)
	{
		FileDto file = TestData.CreateFileDto();

		file
			.Hotkeys
			.Add(new()
			{
				Code = code,
				Id = Guid.NewGuid(),
				Index = 0,
				Mask = mask,
				OwnerId = file.Id
			});

		return file;
	}
	#endregion
}
