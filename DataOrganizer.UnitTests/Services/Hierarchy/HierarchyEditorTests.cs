using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Hierarchy;
using Entities.Enums;
using Entities.Models;
using Hierarchy.Dto;
using Hierarchy.UnitTests.Factories;
using MapsterMapper;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using Shared.Common;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TestSupport.Database;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Hierarchy;

[TestFixture(Description = $@"Tests of ""{nameof(HierarchyEditor)}"" type")]
internal class HierarchyEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="HierarchyEditor.AddAsync" />: a new entity is created and, when a parent is given, linked to it.
	/// </summary>
	[Test]
	public async Task AddAsync_Returns_Entity(
		[Values] EntityKind type,
		[Values] bool hasParent)
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>())
				.Returns(Substitute.For<ExplorerItemBase>());

			builder.RegisterInstance(dbAccess);

			IMapper mapper = Substitute.For<IMapper>();

			mapper
				.Map<ExplorerItemBase, ExplorerItemDtoBase>(Arg.Any<ExplorerItemBase>())
				.Returns(Substitute.For<ExplorerItemDtoBase>());

			builder.RegisterInstance(mapper);
		});

		FolderDto? parent = null;

		if (hasParent)
		{
			parent = new()
			{
				Id = Guid.NewGuid(),
				CreatedAt = default,
				Kind = EntityKind.Folder,
				Index = 0,
				UpdatedAt = default
			};
		}

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [];

		// Act
		ExplorerItemDtoBase? entity = await sut.AddAsync(
			RandomString.Create(10),
			type,
			parent,
			hierarchy);

		// Assert
		entity
			.Should()
			.NotBeNull();

		if (parent is null)
		{
			hierarchy
				.Should()
				.Contain(entity);

			return;
		}

		entity.Parent
			.Should()
			.BeSameAs(parent);

		parent.Children
			.Should()
			.Contain(entity);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteAsync" />: a file or a dataset leaves the database and the hierarchy, while the
	/// folder it lies in stays.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Deletes_A_File_In_Database_And_In_Treeview(
		[Values(EntityKind.File, EntityKind.Dataset)] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder();

		FileEntity file = EntityFactory.CreateFile(parentId: folder.Id);

		ExplorerItemDtoBase toBeDeleted = ItemDtoFactory.CreateFileDto(id: file.Id, kind: kind);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();

			builder
				.RegisterType<HotkeysRepository>()
				.As<IHotkeysRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([folder]);

		await dbAccess.AddFilesAsync([file]);

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5)];

		hierarchy.Add(toBeDeleted);

		// Act
		bool result = await sut.DeleteAsync(toBeDeleted, hierarchy);

		// Assert
		result
			.Should()
			.BeTrue();

		hierarchy
			.Should()
			.NotContain(toBeDeleted);

		FileEntity[] files = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		files
			.Should()
			.BeEmpty();

		FolderEntity[] folders = await dbAccess.GetAllFoldersAsync();

		folders.Select(static x => x.Id)
			.Should()
			.Equal(folder.Id);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteAsync" />: a folder leaves the database together with what lies in it, and leaves
	/// the hierarchy.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Deletes_A_Folder_With_Its_Contents_In_Database_And_In_Treeview()
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder();

		FolderEntity subfolder = EntityFactory.CreateFolder(parentId: folder.Id);

		FileEntity file = EntityFactory.CreateFile(parentId: subfolder.Id);

		ExplorerItemDtoBase toBeDeleted = ItemDtoFactory.CreateFolderDto(id: folder.Id);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();

			builder
				.RegisterType<HotkeysRepository>()
				.As<IHotkeysRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([folder, subfolder]);

		await dbAccess.AddFilesAsync([file]);

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5)];

		hierarchy.Add(toBeDeleted);

		// Act
		bool result = await sut.DeleteAsync(toBeDeleted, hierarchy);

		// Assert
		result
			.Should()
			.BeTrue();

		hierarchy
			.Should()
			.NotContain(toBeDeleted);

		FolderEntity[] folders = await dbAccess.GetAllFoldersAsync();

		folders
			.Should()
			.BeEmpty();

		FileEntity[] files = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteAsync" />: an object the database does not have stays in the hierarchy.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Keeps_The_Entity_When_The_Database_Delete_Fails([Values] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		ExplorerItemDtoBase entity = kind == EntityKind.Folder
			? ItemDtoFactory.CreateFolderDto()
			: ItemDtoFactory.CreateFileDto(kind: kind);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();

			builder
				.RegisterType<HotkeysRepository>()
				.As<IHotkeysRepository>();
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5)];

		hierarchy.Add(entity);

		// Act
		bool result = await sut.DeleteAsync(entity, hierarchy);

		// Assert
		result
			.Should()
			.BeFalse();

		hierarchy
			.Should()
			.Contain(entity);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.RenameAsync" />: renaming to the same name does nothing and leaves the updated date unchanged.
	/// </summary>
	[Test]
	public async Task RenameAsync_Ignores_An_Unchanged_Name()
	{
		// Arrange
		ExplorerItemDtoBase toBeRenamed = Substitute.For<ExplorerItemDtoBase>();

		string newName = RandomString.Create(10);

		toBeRenamed.Name = newName;

		DateTime updatedAt = DateTime.Now;

		toBeRenamed.UpdatedAt = updatedAt;

		using AutoMock mock = AutoMock.GetLoose();

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(toBeRenamed, newName);

		// Assert
		result
			.Should()
			.BeFalse();

		toBeRenamed.UpdatedAt
			.Should()
			.Be(updatedAt);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.RenameAsync" />: the dto name and updated date are changed and persisted in the database,
	/// in the table of the kind of the object.
	/// </summary>
	[Test]
	public async Task RenameAsync_Renames_Dto_And_Updates_Name_In_Database_Entity([Values] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder();

		FileEntity file = EntityFactory.CreateFile();

		ExplorerItemDtoBase dto = kind == EntityKind.Folder
			? ItemDtoFactory.CreateFolderDto(id: folder.Id)
			: ItemDtoFactory.CreateFileDto(id: file.Id, kind: kind);

		string newName = RandomString.Create(10);

		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder.RegisterInstance<TimeProvider>(time);

			builder
				.RegisterType<DbAccess>()
				.As<IDbAccess>();

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		IDbAccess dbAccess = mock.Create<IDbAccess>();

		await dbAccess.AddFoldersAsync([folder]);

		await dbAccess.AddFilesAsync([file]);

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(dto, newName);

		// Assert
		result
			.Should()
			.BeTrue();

		dto.Name
			.Should()
			.Be(newName);

		dto.UpdatedAt
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);

		FolderEntity[] folders = await dbAccess.GetAllFoldersAsync();

		FileEntity[] files = await dbAccess.GetAllFilesAsync(OptionalFileProperties.None);

		ExplorerItemBase[] stored = [.. folders, .. files];

		stored
			.Should()
			.ContainSingle(x => x.Id == dto.Id)
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Name = newName,
				UpdatedAt = time.GetLocalNow().LocalDateTime
			});
	}
	#endregion
}
