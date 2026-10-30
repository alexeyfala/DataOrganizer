using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Hierarchy.UnitTests.Factories;
using HierarchyStorage.Services;
using MapsterMapper;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Enums;
using Repository.Interfaces.Database;
using Shared.Extensions;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Models;

namespace HierarchyStorage.UnitTests.Services;

[TestFixture(Description = $@"Tests of ""{nameof(EntityLoader)}"" type")]
internal class EntityLoaderTests
{
	#region Methods
	/// <summary>
	/// <see cref="EntityLoader.LoadHierarchyAsync" />: a cancelled load is the caller giving up,
	/// so it leaves as a cancellation.
	/// </summary>
	[Test]
	public async Task LoadHierarchyAsync_Passes_A_Cancellation_On()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new OperationCanceledException());

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		Func<Task> act = () => sut.LoadHierarchyAsync();

		// Assert
		await act
			.Should()
			.ThrowAsync<OperationCanceledException>();
	}

	/// <summary>
	/// <see cref="EntityLoader.LoadHierarchyAsync" />: a database that cannot be read is reported
	/// as such, not as a database without objects.
	/// </summary>
	[Test]
	public async Task LoadHierarchyAsync_Reports_A_Database_It_Cannot_Read()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync()
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		LoadedHierarchy? hierarchy = await sut.LoadHierarchyAsync();

		// Assert
		hierarchy
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="EntityLoader.LoadHierarchyAsync" />: loads folders and files and returns a hierarchy containing all of them.
	/// </summary>
	[Test]
	public async Task LoadHierarchyAsync_Returns_All_Folders_And_Files()
	{
		// Arrange
		const int folderCount = 5;

		const int fileCount = 5;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.GetAllFoldersAsync()
				.Returns([.. EntityFactory.CreateFolders(folderCount)]);

			dbAccess
				.GetAllFilesAsync(OptionalFileProperties.None)
				.Returns([.. EntityFactory.CreateFiles(fileCount)]);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<IEnumerable<FileEntity>, FileDto[]>(Arg.Any<IEnumerable<FileEntity>>())
				.Returns([.. ItemDtoFactory.CreateFileDtos(fileCount)]);

			mapper
				.Map<IEnumerable<FolderEntity>, FolderDto[]>(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns([.. ItemDtoFactory.CreateFolderDtos(folderCount)]);

			builder.RegisterInstance(mapper);

			builder.RegisterInstance(dbAccess);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		LoadedHierarchy? hierarchy = await sut.LoadHierarchyAsync();

		// Assert
		hierarchy!.Items
			.Should()
			.HaveCount(folderCount + fileCount);
	}

	/// <summary>
	/// <see cref="EntityLoader.Map" />: the folders stored expanded and the objects stored selected come apart from the
	/// objects.
	/// </summary>
	[Test]
	public void Map_Gives_The_Objects_Stored_Expanded_And_Selected()
	{
		// Arrange
		FolderEntity expanded = EntityFactory.CreateFolder(isExpanded: true);

		FolderEntity selectedFolder = EntityFactory.CreateFolder(isSelected: true);

		FileEntity selectedFile = EntityFactory.CreateFile(isSelected: true);

		using AutoMock mock = AutoMock.GetLoose();

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		LoadedHierarchy loaded = sut.Map(
			[expanded, selectedFolder, EntityFactory.CreateFolder()],
			[selectedFile, EntityFactory.CreateFile()]);

		// Assert
		loaded.ExpandedIds
			.Should()
			.Equal(expanded.Id);

		loaded.SelectedIds
			.Should()
			.BeEquivalentTo([selectedFolder.Id, selectedFile.Id]);
	}

	/// <summary>
	/// <see cref="EntityLoader.Map" />: a folder holding a wrapped key and everything under it are marked as encrypted.
	/// </summary>
	[Test]
	public void Map_Marks_The_Subtree_Of_A_Password_Keeper_As_Encrypted()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateFolderDto();

		keeper.EncryptedDek = RandomValues.CreateBytes(10);

		FolderDto plainFolder = ItemDtoFactory.CreateFolderDto();

		FileDto keptFile = ItemDtoFactory.CreateFileDto();

		keptFile.ParentId = keeper.Id;

		FileDto plainFile = ItemDtoFactory.CreateFileDto();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<IEnumerable<FileEntity>, FileDto[]>(Arg.Any<IEnumerable<FileEntity>>())
				.Returns([keptFile, plainFile]);

			mapper
				.Map<IEnumerable<FolderEntity>, FolderDto[]>(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns([keeper, plainFolder]);

			builder.RegisterInstance(mapper);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		sut.Map([], []);

		// Assert
		keeper.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		keptFile.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Encrypted);

		plainFolder.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.None);

		plainFile.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.None);
	}

	/// <summary>
	/// <see cref="EntityLoader.Map" />: the hotkeys of a file come back in the order of their indexes, which a store or an
	/// import may break.
	/// </summary>
	[Test]
	public void Map_Puts_The_Hotkeys_Of_A_File_In_Their_Order()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		file.Hotkeys.AddRange([CreateHotkey(file.Id, 2), CreateHotkey(file.Id, 0), CreateHotkey(file.Id, 1)]);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<IEnumerable<FileEntity>, FileDto[]>(Arg.Any<IEnumerable<FileEntity>>())
				.Returns([file]);

			mapper
				.Map<IEnumerable<FolderEntity>, FolderDto[]>(Arg.Any<IEnumerable<FolderEntity>>())
				.Returns([]);

			builder.RegisterInstance(mapper);
		});

		EntityLoader sut = mock.Create<EntityLoader>();

		// Act
		sut.Map([], []);

		// Assert
		file.Hotkeys.Select(static x => x.Index)
			.Should()
			.Equal(0, 1, 2);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a hotkey of a file at the given index.
	/// </summary>
	private static HotkeyDto CreateHotkey(Guid ownerId, int index) => new()
	{
		Code = KeyCode.VcA,
		Id = Guid.NewGuid(),
		Index = index,
		Mask = EventMask.LeftCtrl,
		OwnerId = ownerId
	};
	#endregion
}
