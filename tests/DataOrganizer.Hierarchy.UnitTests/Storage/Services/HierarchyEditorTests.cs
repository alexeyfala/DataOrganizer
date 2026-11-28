using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Database.Entities.Enums;
using DataOrganizer.Database.Entities.Models;
using DataOrganizer.Database.Repository.Dto;
using DataOrganizer.Database.Repository.Enums;
using DataOrganizer.Database.Repository.Interfaces;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Database.Repository.Services;
using DataOrganizer.Database.Repository.Services.Database;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Enums;
using DataOrganizer.Hierarchy.Storage.Services;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Factories;
using DataOrganizer.Hierarchy.UnitTests.TestHelpers.Fakes;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.TestSupport.Common;
using DataOrganizer.TestSupport.Database;
using DataOrganizer.TestSupport.Models;
using MapsterMapper;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Hierarchy.UnitTests.Storage.Services;

/// <summary>
/// Tests of <see cref="HierarchyEditor" /> type.
/// </summary>
[TestFixture]
internal sealed class HierarchyEditorTests
{
	#region Methods
	/// <summary>
	/// <see cref="HierarchyEditor.AddAsync" />: a database that answers on another thread leaves the addition to the
	/// hierarchy to the context of the caller.
	/// </summary>
	[Test]
	public void AddAsync_Answered_On_Another_Thread_Adds_The_Object_Through_The_Context_Of_The_Caller()
	{
		// Arrange
		ObservableCollection<ExplorerItemDtoBase> hierarchy = [];

		TaskCompletionSource<ExplorerItemBase> answer = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IMapper mapper = Substitute.For<IMapper>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>(), Arg.Any<CancellationToken>())
				.Returns(answer.Task);

			mapper
				.Map<ExplorerItemBase, ExplorerItemDtoBase>(Arg.Any<ExplorerItemBase>())
				.Returns(ItemDtoFactory.CreateFileDto());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(mapper);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		List<SynchronizationContext?> addedOn = [];

		hierarchy.CollectionChanged += (_, _) => addedOn.Add(SynchronizationContext.Current);

		QueueingSynchronizationContext context = new();

		SynchronizationContext? previous = SynchronizationContext.Current;

		SynchronizationContext.SetSynchronizationContext(context);

		try
		{
			// Act
			_ = sut.AddAsync(RandomString.Create(10), EntityKind.File, null, hierarchy);

			Task
				.Run(() => answer.SetResult(EntityFactory.CreateFile()))
				.Wait();

			context.RunPosted();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previous);
		}

		// Assert
		addedOn
			.Should()
			.Equal(context);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.AddAsync" />: an object added to a folder takes the encryption status of the folder.
	/// </summary>
	[Test]
	public async Task AddAsync_Gives_The_Object_The_Encryption_Status_Of_Its_Folder()
	{
		// Arrange
		FolderDto keeper = ItemDtoFactory.CreateKeeperDto(isUnlocked: true);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			IMapper mapper = Substitute.For<IMapper>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>())
				.Returns(EntityFactory.CreateFile());

			mapper
				.Map<ExplorerItemBase, ExplorerItemDtoBase>(Arg.Any<ExplorerItemBase>())
				.Returns(ItemDtoFactory.CreateFileDto());

			builder.RegisterInstance(dbAccess);

			builder.RegisterInstance(mapper);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		ExplorerItemDtoBase? result = await sut.AddAsync(RandomString.Create(10), EntityKind.File, keeper, []);

		// Assert
		result!.EncryptionStatus
			.Should()
			.Be(EncryptionStatus.Decrypted);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.AddAsync" />: an object added to a folder is placed after the children of the folder,
	/// whatever the size of the tree.
	/// </summary>
	[Test]
	public async Task AddAsync_Places_The_Object_After_The_Children_Of_Its_Folder()
	{
		// Arrange
		FolderDto folder = ItemDtoFactory.CreateFolderDto();

		folder
			.Children
			.AddRange(ItemDtoFactory.CreateFileDtos(2));

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [.. ItemDtoFactory.CreateFolderDtos(5)];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbAccess));

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		await sut.AddAsync(RandomString.Create(10), EntityKind.File, folder, hierarchy);

		// Assert
		await dbAccess
			.Received(1)
			.AddEntityAsync(Arg.Is<AddEntityParameters>(x => x.Index == 2));
	}

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

			IMapper mapper = Substitute.For<IMapper>();

			dbAccess
				.AddEntityAsync(Arg.Any<AddEntityParameters>())
				.Returns(Substitute.For<ExplorerItemBase>());

			mapper
				.Map<ExplorerItemBase, ExplorerItemDtoBase>(Arg.Any<ExplorerItemBase>())
				.Returns(Substitute.For<ExplorerItemDtoBase>());

			builder.RegisterInstance(dbAccess);

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
	/// <see cref="HierarchyEditor.DeleteAsync" />: a database that answers on another thread leaves the removal from the
	/// hierarchy to the context of the caller.
	/// </summary>
	[Test]
	public void DeleteAsync_Answered_On_Another_Thread_Removes_The_Object_Through_The_Context_Of_The_Caller()
	{
		// Arrange
		FileDto file = ItemDtoFactory.CreateFileDto();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [file];

		TaskCompletionSource<bool> answer = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.DeleteAsync(file.Kind, file.Id, Arg.Any<CancellationToken>())
				.Returns(answer.Task);

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		List<SynchronizationContext?> removedOn = [];

		hierarchy.CollectionChanged += (_, _) => removedOn.Add(SynchronizationContext.Current);

		QueueingSynchronizationContext context = new();

		SynchronizationContext? previous = SynchronizationContext.Current;

		SynchronizationContext.SetSynchronizationContext(context);

		try
		{
			// Act
			_ = sut.DeleteAsync(file, hierarchy);

			Task
				.Run(() => answer.SetResult(true))
				.Wait();

			context.RunPosted();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previous);
		}

		// Assert
		removedOn
			.Should()
			.Equal(context);
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
	/// <see cref="HierarchyEditor.DeleteAsync" />: an object whose delete the database refuses stays in the hierarchy.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Keeps_The_Entity_When_The_Database_Throws()
	{
		// Arrange
		ExplorerItemDtoBase entity = ItemDtoFactory.CreateFileDto();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [entity];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.DeleteAsync(Arg.Any<EntityKind>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

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
	/// <see cref="HierarchyEditor.DeleteManyAsync" />: a database that answers on another thread leaves the removal of each
	/// object from the hierarchy to the context of the caller.
	/// </summary>
	[Test]
	public void DeleteManyAsync_Answered_On_Another_Thread_Removes_Each_Object_Through_The_Context_Of_The_Caller()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto second = ItemDtoFactory.CreateFileDto();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [first, second];

		TaskCompletionSource<bool> firstAnswer = new();

		TaskCompletionSource<bool> secondAnswer = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.DeleteAsync(first.Kind, first.Id, Arg.Any<CancellationToken>())
				.Returns(firstAnswer.Task);

			dbAccess
				.DeleteAsync(second.Kind, second.Id, Arg.Any<CancellationToken>())
				.Returns(secondAnswer.Task);

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		List<SynchronizationContext?> removedOn = [];

		hierarchy.CollectionChanged += (_, _) => removedOn.Add(SynchronizationContext.Current);

		QueueingSynchronizationContext context = new();

		SynchronizationContext? previous = SynchronizationContext.Current;

		SynchronizationContext.SetSynchronizationContext(context);

		try
		{
			// Act
			_ = sut.DeleteManyAsync([first, second], hierarchy);

			Task
				.Run(() => firstAnswer.SetResult(true))
				.Wait();

			context.RunPosted();

			Task
				.Run(() => secondAnswer.SetResult(true))
				.Wait();

			context.RunPosted();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previous);
		}

		// Assert
		removedOn
			.Should()
			.Equal(context, context);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteManyAsync" />: every object leaves the database and the place in the hierarchy where
	/// it lies, and all of them are returned.
	/// </summary>
	[Test]
	public async Task DeleteManyAsync_Deletes_Each_Object_In_Database_And_In_Treeview()
	{
		// Arrange
		FileDto nested = ItemDtoFactory.CreateFileDto();

		FolderDto folder = ItemDtoFactory.CreateFolderDto(children: [nested]);

		FileDto root = ItemDtoFactory.CreateFileDto();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [folder, root];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.DeleteAsync(
					Arg.Any<EntityKind>(),
					Arg.Any<Guid>(),
					Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		IReadOnlyList<ExplorerItemDtoBase> result = await sut.DeleteManyAsync([nested, root], hierarchy);

		// Assert
		result
			.Should()
			.Equal(nested, root);

		hierarchy
			.Should()
			.Equal(folder);

		folder.Children
			.Should()
			.BeEmpty();

		await dbAccess
			.Received(1)
			.DeleteAsync(nested.Kind, nested.Id, Arg.Any<CancellationToken>());

		await dbAccess
			.Received(1)
			.DeleteAsync(root.Kind, root.Id, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="HierarchyEditor.DeleteManyAsync" />: the first object the database fails to delete ends the deletion, so
	/// the objects after it stay, and only the objects before it are returned.
	/// </summary>
	[Test]
	public async Task DeleteManyAsync_Stops_At_The_First_Object_It_Fails_To_Delete()
	{
		// Arrange
		FileDto first = ItemDtoFactory.CreateFileDto();

		FileDto failed = ItemDtoFactory.CreateFileDto();

		FileDto last = ItemDtoFactory.CreateFileDto();

		ObservableCollection<ExplorerItemDtoBase> hierarchy = [first, failed, last];

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.DeleteAsync(first.Kind, first.Id, Arg.Any<CancellationToken>())
				.Returns(true);

			// The database has no such object.
			dbAccess
				.DeleteAsync(failed.Kind, failed.Id, Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(dbAccess);
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		IReadOnlyList<ExplorerItemDtoBase> result = await sut.DeleteManyAsync([first, failed, last], hierarchy);

		// Assert
		result
			.Should()
			.Equal(first);

		hierarchy
			.Should()
			.Equal(failed, last);

		await dbAccess
			.DidNotReceive()
			.DeleteAsync(last.Kind, last.Id, Arg.Any<CancellationToken>());
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
	/// <see cref="HierarchyEditor.RenameAsync" />: an object the database does not have keeps its name.
	/// </summary>
	[Test]
	public async Task RenameAsync_Keeps_The_Name_When_The_Database_Has_No_Such_Object()
	{
		// Arrange
		ExplorerItemDtoBase dto = ItemDtoFactory.CreateFileDto();

		string name = dto.Name;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			// The database finds no such object.
			dbAccess
				.RenameAsync(Arg.Any<EntityKind>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
				.Returns(false);

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(dto, RandomString.Create(10));

		// Assert
		result
			.Should()
			.BeFalse();

		dto.Name
			.Should()
			.Be(name);
	}

	/// <summary>
	/// <see cref="HierarchyEditor.RenameAsync" />: an object whose rename the database refuses keeps its name.
	/// </summary>
	[Test]
	public async Task RenameAsync_Keeps_The_Name_When_The_Database_Throws()
	{
		// Arrange
		ExplorerItemDtoBase dto = ItemDtoFactory.CreateFileDto();

		string name = dto.Name;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.RenameAsync(Arg.Any<EntityKind>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbAccess);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		HierarchyEditor sut = mock.Create<HierarchyEditor>();

		// Act
		bool result = await sut.RenameAsync(dto, RandomString.Create(10));

		// Assert
		result
			.Should()
			.BeFalse();

		dto.Name
			.Should()
			.Be(name);
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
