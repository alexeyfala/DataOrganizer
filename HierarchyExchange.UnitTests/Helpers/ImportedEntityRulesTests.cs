using AwesomeAssertions;
using Entities.Models;
using Hierarchy.Dto;
using HierarchyExchange.Helpers;
using HierarchyExchange.UnitTests.Factories;
using SharpHook.Data;
using System;
using TestSupport.Models;

namespace HierarchyExchange.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(ImportedEntityRules)}"" type")]
internal class ImportedEntityRulesTests
{
	#region Methods
	/// <summary>
	/// <see cref="ImportedEntityRules.PlaceAfter" />: an empty tree leaves the objects in their places.
	/// </summary>
	[Test]
	public void PlaceAfter_Leaves_The_Objects_Of_An_Empty_Tree()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		file.Index = 3;

		// Act
		ImportedEntityRules.PlaceAfter([], [], [file]);

		// Assert
		file.Index
			.Should()
			.Be(3);
	}

	/// <summary>
	/// <see cref="ImportedEntityRules.PlaceAfter" />: the top-level objects follow the last object of the tree in their own
	/// order, and the nested ones keep their places.
	/// </summary>
	[Test]
	public void PlaceAfter_Puts_The_Top_Level_Objects_After_The_Tree()
	{
		// Arrange
		FileDto treeFile = ItemDtoFactory.CreateFileDto();

		treeFile.Index = 7;

		FolderEntity folder = EntityFactory.CreateFolder();

		folder.Index = 2;

		FileEntity topFile = EntityFactory.CreateFile();

		topFile.Index = 1;

		FileEntity nestedFile = EntityFactory.CreateFile(parentId: folder.Id);

		nestedFile.Index = 5;

		// Act
		ImportedEntityRules.PlaceAfter([treeFile], [folder], [topFile, nestedFile]);

		// Assert
		(topFile.Index, folder.Index, nestedFile.Index)
			.Should()
			.Be((8, 9, 5));
	}

	/// <summary>
	/// <see cref="ImportedEntityRules.RegenerateIds" />: every object and hotkey gets a new identifier, and the children
	/// and the hotkeys follow their new owners.
	/// </summary>
	[Test]
	public void RegenerateIds_Keeps_The_Links_Under_New_Identifiers()
	{
		// Arrange
		FolderEntity parent = EntityFactory.CreateFolder();

		FolderEntity child = EntityFactory.CreateFolder(parentId: parent.Id);

		FileEntity file = EntityFactory.CreateFile(parentId: child.Id);

		HotkeyEntity hotkey = new()
		{
			Code = KeyCode.VcA,
			Id = Guid.NewGuid(),
			Mask = EventMask.LeftCtrl,
			OwnerId = file.Id
		};

		file.Hotkeys.Add(hotkey);

		Guid[] oldIds = [parent.Id, child.Id, file.Id, hotkey.Id];

		// Act
		ImportedEntityRules.RegenerateIds([parent, child], [file]);

		// Assert
		Guid[] newIds = [parent.Id, child.Id, file.Id, hotkey.Id];

		newIds
			.Should()
			.NotIntersectWith(oldIds);

		(child.ParentId, file.ParentId, hotkey.OwnerId)
			.Should()
			.Be((parent.Id, child.Id, file.Id));
	}

	/// <summary>
	/// <see cref="ImportedEntityRules.SetDates" />: the folders and the files are created and changed at the given time.
	/// </summary>
	[Test]
	public void SetDates_Dates_The_Objects()
	{
		// Arrange
		DateTime now = new(2026, 10, 17, 21, 0, 0);

		FolderEntity folder = EntityFactory.CreateFolder();

		FileEntity file = EntityFactory.CreateFile();

		// Act
		ImportedEntityRules.SetDates([folder], [file], now);

		// Assert
		DateTime[] dates = [folder.CreatedAt, folder.UpdatedAt, file.CreatedAt, file.UpdatedAt];

		dates
			.Should()
			.AllSatisfy(x => x.Should().Be(now));
	}
	#endregion
}
