using AwesomeAssertions;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.Services;
using Mapster;
using MapsterMapper;
using SharpHook.Data;
using System;
using TestSupport.Models;

namespace Hierarchy.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(EntityMappingRegister)}"" type")]
internal class EntityMappingRegisterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityMappingRegister.Register" />: the rules compile, so no mapping fails at the first use.
	/// </summary>
	[Test]
	public void Register_Builds_Rules_That_Compile()
	{
		// Arrange
		TypeAdapterConfig config = new();

		EntityMappingRegister sut = new();

		// Act
		sut.Register(config);

		Action act = () => config.Compile();

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="EntityMappingRegister.Register" />: the parent and the children of a folder are left to the tree,
	/// so the stored ones are not copied.
	/// </summary>
	[Test]
	public void Register_Leaves_The_Parent_And_The_Children_Of_A_Folder_Unset()
	{
		// Arrange
		FolderEntity folder = EntityFactory.CreateFolder();

		folder.Parent = EntityFactory.CreateFolder();

		folder.Children.Add(EntityFactory.CreateFile());

		TypeAdapterConfig config = new();

		EntityMappingRegister sut = new();

		// Act
		sut.Register(config);

		FolderDto result = new Mapper(config).Map<FolderEntity, FolderDto>(folder);

		// Assert
		result.Parent
			.Should()
			.BeNull();

		result.Children
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="EntityMappingRegister.Register" />: the parent of a file is left to the tree, so the stored one is not copied.
	/// </summary>
	[Test]
	public void Register_Leaves_The_Parent_Of_A_File_Unset()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		file.Parent = EntityFactory.CreateFolder();

		TypeAdapterConfig config = new();

		EntityMappingRegister sut = new();

		// Act
		sut.Register(config);

		FileDto result = new Mapper(config).Map<FileEntity, FileDto>(file);

		// Assert
		result.Parent
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EntityMappingRegister.Register" />: a file read as an explorer item becomes a file of the tree.
	/// </summary>
	[Test]
	public void Register_Maps_A_File_Item_To_A_File()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		TypeAdapterConfig config = new();

		EntityMappingRegister sut = new();

		// Act
		sut.Register(config);

		ExplorerItemDtoBase result = new Mapper(config).Map<ExplorerItemBase, ExplorerItemDtoBase>(file);

		// Assert
		result
			.Should()
			.BeOfType<FileDto>()
			.Which
			.Id
			.Should()
			.Be(file.Id);
	}

	/// <summary>
	/// <see cref="EntityMappingRegister.Register" />: a folder read as an explorer item becomes a folder of the tree.
	/// </summary>
	[Test]
	public void Register_Maps_A_Folder_Item_To_A_Folder()
	{
		// Arrange
		FolderEntity folder = EntityFactory.CreateFolder();

		TypeAdapterConfig config = new();

		EntityMappingRegister sut = new();

		// Act
		sut.Register(config);

		ExplorerItemDtoBase result = new Mapper(config).Map<ExplorerItemBase, ExplorerItemDtoBase>(folder);

		// Assert
		result
			.Should()
			.BeOfType<FolderDto>()
			.Which
			.Id
			.Should()
			.Be(folder.Id);
	}

	/// <summary>
	/// <see cref="EntityMappingRegister.Register" />: the hotkeys of a file come along with it.
	/// </summary>
	[Test]
	public void Register_Maps_The_Hotkeys_Of_A_File()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		HotkeyEntity hotkey = new()
		{
			Code = KeyCode.VcA,
			Mask = EventMask.LeftCtrl,
			OwnerId = file.Id
		};

		file.Hotkeys.Add(hotkey);

		TypeAdapterConfig config = new();

		EntityMappingRegister sut = new();

		// Act
		sut.Register(config);

		FileDto result = new Mapper(config).Map<FileEntity, FileDto>(file);

		// Assert
		result.Hotkeys
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				hotkey.Code,
				hotkey.Mask,
				hotkey.OwnerId
			});
	}
	#endregion
}
