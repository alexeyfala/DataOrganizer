using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Enums;
using Repository.Exceptions;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Database;
using TestSupport.Dto;
using TestSupport.Models;

namespace Repository.UnitTests.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
	/// <summary>
	/// <see cref="DbAccess.AddEntityAsync" />: a failed save leaves no entity tracked for the next save to write.
	/// </summary>
	[Test]
	public async Task AddEntityAsync_Clears_The_Tracking_When_The_Save_Fails()
	{
		// Arrange
		AddEntityParameters parameters = new()
		{
			Index = RandomValues.CreateIntFrom10To100(),
			Kind = EntityKind.Folder,
			Name = RandomString.Create(10),
			ParentId = null
		};

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.SaveChangesAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Func<Task> act = () => sut.AddEntityAsync(parameters);

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.AddEntityAsync" />: creates a folder or file entity with the supplied parameters, saves changes and
	/// clears the tracking.
	/// </summary>
	[Test]
	public async Task AddEntityAsync_Returns_Entity([Values] EntityKind type)
	{
		// Arrange
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		AddEntityParameters parameters = new()
		{
			Index = RandomValues.CreateIntFrom10To100(),
			Kind = type,
			Name = RandomString.Create(10),
			ParentId = Guid.NewGuid()
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(folderRepository);

			builder.RegisterInstance(fileRepository);

			builder
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		ExplorerItemBase? entity = await sut.AddEntityAsync(parameters);

		// Assert
		entity
			.Should()
			.NotBeNull();

		entity.Id
			.Should()
			.NotBeEmpty();

		// A struct is compared by value unless asked otherwise; the entity carries no contents, so
		// the members the two do not share are left out.
		entity
			.Should()
			.BeEquivalentTo(
				parameters,
				static options => options
					.ComparingByMembers<AddEntityParameters>()
					.ExcludingMissingMembers());

		await dbContextService
			.Received(1)
			.SaveChangesAsync();

		dbContextService
			.Received(1)
			.ClearTracking();

		if (type == EntityKind.Folder)
		{
			entity
				.Should()
				.BeOfType<FolderEntity>();

			await folderRepository
				.Received(1)
				.AddAsync(Arg.Any<FolderEntity>());
		}
		else
		{
			entity
				.Should()
				.BeOfType<FileEntity>();

			await fileRepository
				.Received(1)
				.AddAsync(Arg.Any<FileEntity>());
		}
	}

	/// <summary>
	/// <see cref="DbAccess.AddEntityAsync" />: a new folder or file is created and updated at the current local time.
	/// </summary>
	[Test]
	public async Task AddEntityAsync_Stamps_The_Entity_With_The_Current_Time([Values] EntityKind type)
	{
		// Arrange
		FakeTimeProvider time = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance<TimeProvider>(time));

		DbAccess sut = mock.Create<DbAccess>();

		AddEntityParameters parameters = new()
		{
			Index = RandomValues.CreateIntFrom10To100(),
			Kind = type,
			Name = RandomString.Create(10),
			ParentId = Guid.NewGuid()
		};

		// Act
		ExplorerItemBase entity = await sut.AddEntityAsync(parameters);

		// Assert
		entity.CreatedAt
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);

		entity.UpdatedAt
			.Should()
			.Be(time.GetLocalNow().LocalDateTime);
	}

	/// <summary>
	/// <see cref="DbAccess.AddFilesAsync" />: adds the files via the repository, saves changes and clears the tracking.
	/// </summary>
	[Test]
	public async Task AddFilesAsync_Adds_Files_To_Database()
	{
		// Arrange
		FileEntity[] files = [.. EntityFactory.CreateFiles(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IFileRepository repository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.AddFilesAsync(files);

		// Assert
		await repository
			.Received(1)
			.AddRangeAsync(Arg.Any<IEnumerable<FileEntity>>());

		await dbContextService
			.Received(1)
			.SaveChangesAsync();

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.AddFilesAsync" />: a failed save leaves no file tracked for the next save to write.
	/// </summary>
	[Test]
	public async Task AddFilesAsync_Clears_The_Tracking_When_The_Save_Fails()
	{
		// Arrange
		FileEntity[] files = [.. EntityFactory.CreateFiles(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.SaveChangesAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Func<Task> act = () => sut.AddFilesAsync(files);

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.AddFilesAsync" />, <see cref="DbAccess.AddEntityAsync" />: a file the database refuses is not
	/// written again by the next save, which goes through.
	/// </summary>
	[Test]
	public async Task AddFilesAsync_Leaves_Nothing_Behind_When_The_Save_Fails()
	{
		// Arrange
		using TestDatabase database = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

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
				.RegisterType<FakeTimeProvider>()
				.As<TimeProvider>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// A file whose folder does not exist breaks the foreign key.
		Func<Task> refused = () => sut.AddFilesAsync([EntityFactory.CreateFile(parentId: Guid.NewGuid())]);

		await refused
			.Should()
			.ThrowAsync<DbUpdateException>();

		// Act
		await sut.AddEntityAsync(new()
		{
			Index = RandomValues.CreateIntFrom10To100(),
			Kind = EntityKind.Folder,
			Name = RandomString.Create(10),
			ParentId = null
		});

		// Assert
		FolderEntity[] folders = await sut.GetAllFoldersAsync();

		FileEntity[] files = await sut.GetAllFilesAsync(OptionalFileProperties.None);

		folders
			.Should()
			.ContainSingle();

		files
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="DbAccess.AddFoldersAsync" />: adds the folders via the repository, saves changes and clears the tracking.
	/// </summary>
	[Test]
	public async Task AddFoldersAsync_Adds_Folders_To_Database()
	{
		// Arrange
		FolderEntity[] folders = [.. EntityFactory.CreateFolders(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IFolderRepository repository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.AddFoldersAsync(folders);

		// Assert
		await repository
			.Received(1)
			.AddRangeAsync(Arg.Any<IEnumerable<FolderEntity>>());

		await dbContextService
			.Received(1)
			.SaveChangesAsync();

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.AddFoldersAsync" />: a failed save leaves no folder tracked for the next save to write.
	/// </summary>
	[Test]
	public async Task AddFoldersAsync_Clears_The_Tracking_When_The_Save_Fails()
	{
		// Arrange
		FolderEntity[] folders = [.. EntityFactory.CreateFolders(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.SaveChangesAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Func<Task> act = () => sut.AddFoldersAsync(folders);

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.ClearDatabaseAsync" />: deletes the database and recreates it via migrations or creation depending on migration support.
	/// </summary>
	[Test]
	public async Task ClearDatabaseAsync_Recreates_Database([Values] bool useMigrations)
	{
		// Arrange
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.HasMigrations()
				.Returns(useMigrations);

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.ClearDatabaseAsync();

		// Assert
		dbContextService
			.Received(1)
			.EnsureDeleted();

		if (useMigrations)
		{
			dbContextService
				.Received(1)
				.Migrate();
		}
		else
		{
			dbContextService
				.Received()
				.EnsureCreated();
		}
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: migrates or creates the database depending on migration support.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Connects_To_Database([Values] bool useMigrations)
	{
		// Arrange
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.HasMigrations()
				.Returns(useMigrations);

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.Connected);

		if (useMigrations)
		{
			await dbContextService
				.Received(1)
				.MigrateAsync();
		}
		else
		{
			await dbContextService
				.Received()
				.EnsureCreatedAsync();
		}
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a database that opened is swept once, so the pages freed
	/// by earlier deletions stop carrying what was deleted.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Erases_The_Free_Pages_Once()
	{
		// Arrange
		IDbMaintenance dbMaintenance = Substitute.For<IDbMaintenance>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbMaintenance));

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.Connected);

		await dbMaintenance
			.Received(1)
			.EraseFreePagesOnceAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a file that is not a database is not migrated.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Reports_A_File_It_Cannot_Open()
	{
		// Arrange
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileSystem fileSystem = Substitute.For<IFileSystem>();

			dbContextService
				.HasMigrations()
				.Returns(true);

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(true);

			fileSystem
				.OpenRead(Arg.Any<string>())
				.Returns(_ => new MemoryStream(new byte[32]));

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileSystem);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.FileUnreadable);

		await dbContextService
			.DidNotReceive()
			.MigrateAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a database that cannot be created is reported as unreadable.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Reports_An_Unusable_Database()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.EnsureCreatedAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.FileUnreadable);
	}

	/// <summary>
	/// <see cref="DbAccess.IsWritable" />: a failed connect closes the database for writing until the next one.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Shuts_The_Writes_Down_After_A_Failure()
	{
		// Arrange
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.EnsureCreatedAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(folderRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Assert
		sut
			.IsWritable
			.Should()
			.BeTrue();

		// Act
		await sut.ConnectAsync();

		// Assert
		sut
			.IsWritable
			.Should()
			.BeFalse();

		await AssertRefusedAsync(() => sut.AddEntityAsync(new()
		{
			Index = 0,
			Kind = EntityKind.Folder,
			Name = RandomString.Create(10),
			ParentId = Guid.NewGuid()
		}));

		await AssertRefusedAsync(() => sut.AddFilesAsync([]));

		await AssertRefusedAsync(() => sut.AddFoldersAsync([]));

		await AssertRefusedAsync(() => sut.ClearDatabaseAsync());

		await AssertRefusedAsync(() => sut.DeleteAsync(EntityKind.File, Guid.NewGuid()));

		await AssertRefusedAsync(() => sut.DeleteAsync(EntityKind.Folder, Guid.NewGuid()));

		await AssertRefusedAsync(() => sut.DeleteHotkeysAsync(Guid.NewGuid()));

		await AssertRefusedAsync(() => sut.RenameAsync(EntityKind.File, Guid.NewGuid(), RandomString.Create(10), default));

		await AssertRefusedAsync(() => sut.ReplaceHotkeysAsync(Guid.NewGuid(), []));

		await AssertRefusedAsync(() => sut.RestoreFromBackupAsync(RandomString.Create(10)));

		await AssertRefusedAsync(() => sut.UpdateEncryptedDekAsync(Guid.NewGuid(), []));

		await AssertRefusedAsync(() => sut.UpdateFileContentsAsync(Guid.NewGuid(), [], null));

		await AssertRefusedAsync(() => sut.UpdateFileEditorStateAsync(Guid.NewGuid(), RandomString.Create(10)));

		await AssertRefusedAsync(() => sut.UpdateIsExpandedAsync(Guid.NewGuid(), true));

		await AssertRefusedAsync(() => sut.UpdateIsFavoriteAsync(Guid.NewGuid(), true));

		await AssertRefusedAsync(() => sut.UpdateIsSelectedAsync(EntityKind.Folder, Guid.NewGuid(), true));

		await AssertRefusedAsync(() => sut.UpdateNoteAsync(EntityKind.Folder, Guid.NewGuid(), null, default));

		await AssertRefusedAsync(() => sut.UpdateProtectionAsync(new()
		{
			Contents = new Dictionary<Guid, byte[]>(),
			EncryptedDek = null,
			FileNotes = new Dictionary<Guid, byte[]>(),
			FolderId = Guid.NewGuid(),
			FolderNotes = new Dictionary<Guid, byte[]>(),
			UpdatedAt = default
		}));

		await dbContextService
			.DidNotReceive()
			.SaveChangesAsync(Arg.Any<CancellationToken>());

		static async Task AssertRefusedAsync(Func<Task> write)
		{
			await write
				.Should()
				.ThrowAsync<DatabaseNotWritableException>();
		}

		await fileRepository
			.DidNotReceiveWithAnyArgs()
			.AddRangeAsync(default!);

		await folderRepository
			.DidNotReceiveWithAnyArgs()
			.AddAsync(default!);
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a failing housekeeping step leaves the database usable.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Survives_A_Failed_Housekeeping_Step()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbMaintenance dbMaintenance = Substitute.For<IDbMaintenance>();

			dbMaintenance
				.When(x => x.ErasePendingBackups())
				.Throw(new IOException());

			dbMaintenance
				.EraseFreePagesOnceAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new IOException());

			builder.RegisterInstance(dbMaintenance);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.Connected);
	}

	/// <summary>
	/// <see cref="DbAccess.CountOfAsync" />: returns the count of entities matching the predicate.
	/// </summary>
	[Test]
	public async Task CountOfAsync_Returns_Count_Of_Matching_Entities()
	{
		// Arrange
		const int expectedCount = 7;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerItemRepository repository = Substitute.For<IExplorerItemRepository>();

			repository
				.CountOfAsync(Arg.Any<Expression<Func<ExplorerItemBase, bool>>>())
				.Returns(expectedCount);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		int result = await sut.CountOfAsync(x => x.Name != null);

		// Assert
		result
			.Should()
			.Be(expectedCount);
	}

	/// <summary>
	/// <see cref="DbAccess.CreateBackupAsync" />: without a database file there is nothing to copy,
	/// so no folder for the copies is made either.
	/// </summary>
	[Test]
	public async Task CreateBackupAsync_Returns_Null_Without_A_Database_File()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(TestPath.Combine("Database", "DataOrganizer.sqlite"));

			fileSystem
				.FileExists(Arg.Any<string>())
				.Returns(false);

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileSystem);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DatabaseBackup? backup = await sut.CreateBackupAsync();

		// Assert
		backup
			.Should()
			.BeNull();

		fileSystem
			.DidNotReceive()
			.CreateDirectory(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteAsync" />: removes the file's hotkeys and the file itself, returning true.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Deletes_File_From_Database([Values(EntityKind.File, EntityKind.Dataset)] EntityKind kind)
	{
		// Arrange
		IHotkeysRepository hotkeysRepository = Substitute.For<IHotkeysRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			fileRepository
				.RemoveAsync(Arg.Any<Guid>())
				.Returns(1);

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(hotkeysRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteAsync(kind, default);

		// Assert
		result
			.Should()
			.BeTrue();

		await hotkeysRepository
			.Received(1)
			.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>());

		await fileRepository
			.Received(1)
			.RemoveAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteAsync" />: removes an empty folder without touching file or hotkey repositories.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Deletes_Folder_From_Database()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		Guid[] subtreeIds = [folderId];

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		IHotkeysRepository hotkeysRepository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			folderRepository
				.GetFolderSubtreeIdsAsync(folderId)
				.Returns(ToAsyncEnumerable(subtreeIds));

			fileRepository
				.GetFileIdsAsync(Arg.Any<Guid[]>())
				.Returns([]);

			folderRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(subtreeIds.Length);

			builder.RegisterInstance(folderRepository);

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(hotkeysRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteAsync(EntityKind.Folder, folderId);

		// Assert
		result
			.Should()
			.BeTrue();

		await folderRepository
			.Received(1)
			.RemoveRangeByIdsAsync(Arg.Any<Guid[]>());

		await hotkeysRepository
			.DidNotReceive()
			.RemoveRangeByOwnerIdsAsync(Arg.Any<Guid[]>());

		await fileRepository
			.DidNotReceive()
			.RemoveRangeByIdsAsync(Arg.Any<Guid[]>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteAsync" />: removes the whole subtree including nested folders, files and their hotkeys.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Removes_Folder_With_Nested_Folders_And_Files()
	{
		// Arrange
		Guid rootId = Guid.NewGuid();

		Guid[] subtreeIds = [rootId, Guid.NewGuid(), Guid.NewGuid()];

		Guid[] fileIds = [.. RandomValues.CreateGuids(4)];

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		IHotkeysRepository hotkeysRepository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			folderRepository
				.GetFolderSubtreeIdsAsync(rootId)
				.Returns(ToAsyncEnumerable(subtreeIds));

			fileRepository
				.GetFileIdsAsync(Arg.Any<Guid[]>())
				.Returns(fileIds);

			hotkeysRepository
				.RemoveRangeByOwnerIdsAsync(Arg.Any<Guid[]>())
				.Returns(fileIds.Length);

			fileRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(fileIds.Length);

			folderRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(subtreeIds.Length);

			builder.RegisterInstance(folderRepository);

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(hotkeysRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteAsync(EntityKind.Folder, rootId);

		// Assert
		result
			.Should()
			.BeTrue();

		await hotkeysRepository
			.Received(1)
			.RemoveRangeByOwnerIdsAsync(fileIds);

		await fileRepository
			.Received(1)
			.RemoveRangeByIdsAsync(fileIds);

		await folderRepository
			.Received(1)
			.RemoveRangeByIdsAsync(Arg.Is<Guid[]>(x => x.SequenceEqual(subtreeIds)));
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteAsync" />: returns false when no file rows are removed.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Returns_False_When_File_Does_Not_Exist()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository fileRepository = Substitute.For<IFileRepository>();

			fileRepository
				.RemoveAsync(Arg.Any<Guid>())
				.Returns(0);

			builder.RegisterInstance(fileRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteAsync(EntityKind.File, Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteAsync" />: returns false when no folder rows are removed.
	/// </summary>
	[Test]
	public async Task DeleteAsync_Returns_False_When_Folder_Does_Not_Exist()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

			IFileRepository fileRepository = Substitute.For<IFileRepository>();

			folderRepository
				.GetFolderSubtreeIdsAsync(folderId)
				.Returns(ToAsyncEnumerable<Guid>([]));

			fileRepository
				.GetFileIdsAsync(Arg.Any<Guid[]>())
				.Returns([]);

			folderRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(0);

			builder.RegisterInstance(folderRepository);

			builder.RegisterInstance(fileRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteAsync(EntityKind.Folder, folderId);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteHotkeysAsync" />: removes all hotkeys owned by the given id and returns true.
	/// </summary>
	[Test]
	public async Task DeleteHotkeysAsync_Deletes_Hotkeys_From_Database()
	{
		// Arrange
		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>())
				.Returns(3);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteHotkeysAsync(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received(1)
			.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteHotkeysAsync" />: returns false when no hotkey rows are removed.
	/// </summary>
	[Test]
	public async Task DeleteHotkeysAsync_Returns_False_When_No_Hotkeys_Exist()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

			repository
				.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>())
				.Returns(0);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteHotkeysAsync(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.Dispose" />: calling it twice does not throw.
	/// </summary>
	[Test]
	public void Dispose_Is_Idempotent()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Action act = () =>
		{
			sut.Dispose();

			sut.Dispose();
		};

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="DbAccess.ExistsAsync" />: reports a wait the token cancelled and stays usable afterwards.
	/// </summary>
	[Test]
	public async Task ExistsAsync_Reports_A_Cancelled_Wait_And_Survives_It()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerItemRepository repository = Substitute.For<IExplorerItemRepository>();

			repository
				.ExistsAsync(Arg.Any<Expression<Func<ExplorerItemBase, bool>>>())
				.Returns(true);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		using CancellationTokenSource cancellation = new();

		cancellation.Cancel();

		// Act
		Func<Task> act = () => sut.ExistsAsync(id, cancellation.Token);

		// Assert
		await act
			.Should()
			.ThrowAsync<OperationCanceledException>();

		(await sut.ExistsAsync(id))
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DbAccess.ExistsAsync" />: returns true when an entity matches the id.
	/// </summary>
	[Test]
	public async Task ExistsAsync_Returns_True_When_Entity_Exists()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerItemRepository repository = Substitute.For<IExplorerItemRepository>();

			repository
				.ExistsAsync(Arg.Any<Expression<Func<ExplorerItemBase, bool>>>())
				.Returns(true);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.ExistsAsync(id);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DbAccess.GetAllFilesAsync" />: returns all files from the repository.
	/// </summary>
	[Test]
	public async Task GetAllFilesAsync_Returns_Files()
	{
		// Arrange
		FileEntity[] expectedResult = [.. EntityFactory.CreateFiles(100)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.GetAllAsync(OptionalFileProperties.None)
				.Returns(expectedResult);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		FileEntity[] result = await sut.GetAllFilesAsync(OptionalFileProperties.None);

		// Assert
		result
			.Should()
			.BeEquivalentTo(expectedResult);
	}

	/// <summary>
	/// <see cref="DbAccess.GetAllFoldersAsync" />: returns all folders from the repository.
	/// </summary>
	[Test]
	public async Task GetAllFoldersAsync_Returns_Folders()
	{
		// Arrange
		FolderEntity[] expectedResult = [.. EntityFactory.CreateFolders(100)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderRepository repository = Substitute.For<IFolderRepository>();

			repository
				.GetAllAsync()
				.Returns(expectedResult);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		FolderEntity[] result = await sut.GetAllFoldersAsync();

		// Assert
		result
			.Should()
			.BeEquivalentTo(expectedResult);
	}

	/// <summary>
	/// <see cref="DbAccess.GetFileContentsAsync" />: returns a valid result with the file's id and contents.
	/// </summary>
	[Test]
	public async Task GetFileContentsAsync_Returns_File_Contents()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.GetContentsAsync(Arg.Any<Guid>())
				.Returns(file.Contents);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		ValidatedContents result = await sut.GetFileContentsAsync(file.Id);

		// Assert
		result.IsValid
			.Should()
			.BeTrue();

		result.Id
			.Should()
			.Be(file.Id);

		result.Contents
			.Should()
			.BeEquivalentTo(file.Contents);
	}

	/// <summary>
	/// <see cref="DbAccess.GetFileContentsRangeAsync" />: yields valid contents for each requested identifier.
	/// </summary>
	[Test]
	public async Task GetFileContentsRangeAsync_Yields_Pair_For_Each_Identifier()
	{
		// Arrange
		FileEntity[] files = [.. EntityFactory.CreateFiles(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			foreach (FileEntity file in files)
			{
				repository
					.GetContentsAsync(file.Id)
					.Returns(file.Contents);
			}

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		List<ValidatedContents> results = [];

		await foreach (ValidatedContents contents in sut.GetFileContentsRangeAsync(files.Select(x => x.Id)))
		{
			results.Add(contents);
		}

		// Assert
		results
			.Should()
			.HaveCount(files.Length);

		results
			.Should()
			.OnlyContain(x => x.IsValid);
	}

	/// <summary>
	/// <see cref="DbAccess.GetFileEditorStateAsync" />: returns the stored editor state of the file.
	/// </summary>
	[Test]
	public async Task GetFileEditorStateAsync_Returns_The_Editor_State()
	{
		// Arrange
		FileEntity file = EntityFactory.CreateFile();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.GetEditorStateAsync(Arg.Any<Guid>())
				.Returns(file.EditorState);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		string? result = await sut.GetFileEditorStateAsync(file.Id);

		// Assert
		result
			.Should()
			.Be(file.EditorState);
	}

	/// <summary>
	/// <see cref="DbAccess.RenameAsync" />: an object the database does not have is reported as not renamed.
	/// </summary>
	[Test]
	public async Task RenameAsync_Returns_False_For_An_Object_Not_In_The_Database([Values] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

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

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.RenameAsync(
			kind,
			Guid.NewGuid(),
			RandomString.Create(10),
			default);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.RenameAsync" />: an object gets its new name and date of change in the table of its kind.
	/// </summary>
	[Test]
	public async Task RenameAsync_Writes_The_Name_And_The_Date([Values] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder();

		FileEntity file = EntityFactory.CreateFile();

		Guid id = kind == EntityKind.Folder ? folder.Id : file.Id;

		string name = RandomString.Create(10);

		// The database keeps dates to the second.
		DateTime updatedAt = new(2020, 1, 2, 3, 4, 5);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

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

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFoldersAsync([folder]);

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.RenameAsync(kind, id, name, updatedAt);

		// Assert
		result
			.Should()
			.BeTrue();

		ExplorerItemBase[] stored =
		[
			.. await sut.GetAllFoldersAsync(),
			.. await sut.GetAllFilesAsync(OptionalFileProperties.None)
		];

		stored
			.Should()
			.ContainSingle(x => x.Id == id)
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Name = name,
				UpdatedAt = updatedAt
			});
	}

	/// <summary>
	/// <see cref="DbAccess.ReplaceHotkeysAsync" />: a failed save leaves no hotkey tracked for the next save to write.
	/// </summary>
	[Test]
	public async Task ReplaceHotkeysAsync_Clears_The_Tracking_When_The_Save_Fails()
	{
		// Arrange
		KeyStroke[] keyStrokes = [.. KeyStrokeFactory.CreateKeyStrokes(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The stub runs the body of the transaction instead of a database.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(x => x.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

			dbContextService
				.SaveChangesAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Func<Task> act = () => sut.ReplaceHotkeysAsync(Guid.NewGuid(), keyStrokes);

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.ReplaceHotkeysAsync" />: the old hotkeys of the file are deleted and one new hotkey per key
	/// stroke is added and saved, and the new ones come back.
	/// </summary>
	[Test]
	public async Task ReplaceHotkeysAsync_Replaces_The_Hotkeys_Of_The_File()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		KeyStroke[] keyStrokes = [.. KeyStrokeFactory.CreateKeyStrokes(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The stub runs the body of the transaction instead of a database.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(x => x.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		HotkeyEntity[] result = await sut.ReplaceHotkeysAsync(fileId, keyStrokes);

		// Assert
		result
			.Should()
			.HaveCount(keyStrokes.Length)
			.And
			.OnlyContain(x => x.OwnerId == fileId);

		await repository
			.Received(1)
			.RemoveRangeByOwnerIdAsync(fileId, Arg.Any<CancellationToken>());

		await repository
			.Received(keyStrokes.Length)
			.AddAsync(Arg.Any<HotkeyEntity>(), Arg.Any<CancellationToken>());

		await dbContextService
			.Received(1)
			.SaveChangesAsync(Arg.Any<CancellationToken>());

		dbContextService
			.Received(1)
			.ClearTracking();
	}

	/// <summary>
	/// <see cref="DbAccess.ReplaceHotkeysAsync" />: the deletion, the addition and the save all happen inside the
	/// transaction, so a failure of any of them leaves the old hotkeys.
	/// </summary>
	[Test]
	public async Task ReplaceHotkeysAsync_Writes_Nothing_Outside_The_Transaction()
	{
		// Arrange
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The transaction never runs its body, though that is also the default answer.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(Task.CompletedTask);

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.ReplaceHotkeysAsync(Guid.NewGuid(), [.. KeyStrokeFactory.CreateKeyStrokes(2)]);

		// Assert
		await repository
			.DidNotReceive()
			.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

		await repository
			.DidNotReceive()
			.AddAsync(Arg.Any<HotkeyEntity>(), Arg.Any<CancellationToken>());

		await dbContextService
			.DidNotReceive()
			.SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateEncryptedDekAsync" />: the folder gets its new wrapped key.
	/// </summary>
	[Test]
	public async Task UpdateEncryptedDekAsync_Writes_The_Wrapped_Key()
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder(encryptedDek: RandomValues.CreateBytes(10));

		byte[] encryptedDek = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFoldersAsync([folder]);

		// Act
		bool result = await sut.UpdateEncryptedDekAsync(folder.Id, encryptedDek);

		// Assert
		result
			.Should()
			.BeTrue();

		FolderEntity[] stored = await sut.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.EncryptedDek
			.Should()
			.Equal(encryptedDek);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileContentsAsync" />: contents written without a date leave the date of the file as it
	/// is.
	/// </summary>
	[Test]
	public async Task UpdateFileContentsAsync_Keeps_The_Date_Without_One()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: RandomValues.CreateBytes(10));

		// The database keeps dates to the second.
		DateTime updatedAt = new(2020, 1, 2, 3, 4, 5);

		file.UpdatedAt = updatedAt;

		byte[] contents = RandomValues.CreateBytes(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.UpdateFileContentsAsync(file.Id, contents, null);

		// Assert
		result
			.Should()
			.BeTrue();

		FileEntity[] stored = await sut.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Contents = contents,
				UpdatedAt = updatedAt
			});
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileContentsAsync" />: a file the database does not have is reported as not written.
	/// </summary>
	[Test]
	public async Task UpdateFileContentsAsync_Returns_False_For_A_File_Not_In_The_Database()
	{
		// Arrange
		using TestDatabase database = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFileContentsAsync(
			Guid.NewGuid(),
			RandomValues.CreateBytes(10),
			null);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileContentsAsync" />: the file gets its new contents and date of change.
	/// </summary>
	[Test]
	public async Task UpdateFileContentsAsync_Writes_The_Contents_And_The_Date()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(contents: RandomValues.CreateBytes(10));

		byte[] contents = RandomValues.CreateBytes(10);

		// The database keeps dates to the second.
		DateTime updatedAt = new(2020, 1, 2, 3, 4, 5);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.UpdateFileContentsAsync(file.Id, contents, updatedAt);

		// Assert
		result
			.Should()
			.BeTrue();

		FileEntity[] stored = await sut.GetAllFilesAsync(OptionalFileProperties.Contents);

		stored
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Contents = contents,
				UpdatedAt = updatedAt
			});
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileEditorStateAsync" />: the file gets its new editor state.
	/// </summary>
	[Test]
	public async Task UpdateFileEditorStateAsync_Writes_The_Editor_State()
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(editorState: RandomString.Create(10));

		string editorState = RandomString.Create(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.UpdateFileEditorStateAsync(file.Id, editorState);

		// Assert
		result
			.Should()
			.BeTrue();

		FileEntity[] stored = await sut.GetAllFilesAsync(OptionalFileProperties.EditorState);

		stored
			.Should()
			.ContainSingle()
			.Which
			.EditorState
			.Should()
			.Be(editorState);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateIsExpandedAsync" />: the folder gets its new expansion.
	/// </summary>
	[Test]
	public async Task UpdateIsExpandedAsync_Writes_The_Expansion([Values] bool isExpanded)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder(isExpanded: !isExpanded);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FolderRepository>()
				.As<IFolderRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFoldersAsync([folder]);

		// Act
		bool result = await sut.UpdateIsExpandedAsync(folder.Id, isExpanded);

		// Assert
		result
			.Should()
			.BeTrue();

		FolderEntity[] stored = await sut.GetAllFoldersAsync();

		stored
			.Should()
			.ContainSingle()
			.Which
			.IsExpanded
			.Should()
			.Be(isExpanded);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateIsFavoriteAsync" />: the file gets its new favorite mark.
	/// </summary>
	[Test]
	public async Task UpdateIsFavoriteAsync_Writes_The_Favorite_Mark([Values] bool isFavorite)
	{
		// Arrange
		using TestDatabase database = new();

		FileEntity file = EntityFactory.CreateFile(isFavorite: !isFavorite);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

			builder
				.RegisterType<DbContextService>()
				.As<IDbContextService>();

			builder
				.RegisterType<FileRepository>()
				.As<IFileRepository>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.UpdateIsFavoriteAsync(file.Id, isFavorite);

		// Assert
		result
			.Should()
			.BeTrue();

		FileEntity[] stored = await sut.GetAllFilesAsync(OptionalFileProperties.None);

		stored
			.Should()
			.ContainSingle()
			.Which
			.IsFavorite
			.Should()
			.Be(isFavorite);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateIsSelectedAsync" />: an object gets its new selection in the table of its kind.
	/// </summary>
	[Test]
	public async Task UpdateIsSelectedAsync_Writes_The_Selection([Values] EntityKind kind, [Values] bool isSelected)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder(isSelected: !isSelected);

		FileEntity file = EntityFactory.CreateFile(isSelected: !isSelected);

		Guid id = kind == EntityKind.Folder ? folder.Id : file.Id;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

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

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFoldersAsync([folder]);

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.UpdateIsSelectedAsync(kind, id, isSelected);

		// Assert
		result
			.Should()
			.BeTrue();

		ExplorerItemBase[] stored =
		[
			.. await sut.GetAllFoldersAsync(),
			.. await sut.GetAllFilesAsync(OptionalFileProperties.None)
		];

		stored
			.Should()
			.ContainSingle(x => x.Id == id)
			.Which
			.IsSelected
			.Should()
			.Be(isSelected);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateNoteAsync" />: an object gets its new note and date of change in the table of its kind.
	/// </summary>
	[Test]
	public async Task UpdateNoteAsync_Writes_The_Note_And_The_Date([Values] EntityKind kind)
	{
		// Arrange
		using TestDatabase database = new();

		FolderEntity folder = EntityFactory.CreateFolder(note: RandomValues.CreateBytes(10));

		FileEntity file = EntityFactory.CreateFile(note: RandomValues.CreateBytes(10));

		Guid id = kind == EntityKind.Folder ? folder.Id : file.Id;

		byte[] note = RandomValues.CreateBytes(10);

		// The database keeps dates to the second.
		DateTime updatedAt = new(2020, 1, 2, 3, 4, 5);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

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

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFoldersAsync([folder]);

		await sut.AddFilesAsync([file]);

		// Act
		bool result = await sut.UpdateNoteAsync(kind, id, note, updatedAt);

		// Assert
		result
			.Should()
			.BeTrue();

		ExplorerItemBase[] stored =
		[
			.. await sut.GetAllFoldersAsync(),
			.. await sut.GetAllFilesAsync(OptionalFileProperties.None)
		];

		stored
			.Should()
			.ContainSingle(x => x.Id == id)
			.Which
			.Should()
			.BeEquivalentTo(new
			{
				Note = note,
				UpdatedAt = updatedAt
			});
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateProtectionAsync" />: a change with nothing but the key of the folder is not a failure.
	/// </summary>
	[Test]
	public async Task UpdateProtectionAsync_Accepts_An_Empty_Set_Of_Updates()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			// The stub runs the body of the transaction instead of a database.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(x => x.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Func<Task> act = () => sut.UpdateProtectionAsync(new()
		{
			Contents = new Dictionary<Guid, byte[]>(),
			EncryptedDek = null,
			FileNotes = new Dictionary<Guid, byte[]>(),
			FolderId = Guid.NewGuid(),
			FolderNotes = new Dictionary<Guid, byte[]>(),
			UpdatedAt = default
		});

		// Assert
		await act
			.Should()
			.NotThrowAsync();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateProtectionAsync" />: a write that failed inside the transaction reaches the caller.
	/// </summary>
	[Test]
	public async Task UpdateProtectionAsync_Lets_A_Failed_Write_Out()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			IFileRepository fileRepository = Substitute.For<IFileRepository>();

			// The stub runs the body of the transaction instead of a database.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(x => x.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

			fileRepository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
					Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		Func<Task> act = () => sut.UpdateProtectionAsync(new()
		{
			Contents = new Dictionary<Guid, byte[]>
			{
				[Guid.NewGuid()] = RandomValues.CreateBytes(10)
			},
			EncryptedDek = null,
			FileNotes = new Dictionary<Guid, byte[]>(),
			FolderId = Guid.NewGuid(),
			FolderNotes = new Dictionary<Guid, byte[]>(),
			UpdatedAt = default
		});

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateProtectionAsync" />: every write of the change belongs to the one transaction, so none is
	/// made outside of it.
	/// </summary>
	[Test]
	public async Task UpdateProtectionAsync_Writes_Only_Inside_The_Transaction()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		Guid folderId = Guid.NewGuid();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			// The transaction never runs its body, though that is also the default answer.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(Task.CompletedTask);

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(folderRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.UpdateProtectionAsync(new()
		{
			Contents = new Dictionary<Guid, byte[]>
			{
				[fileId] = RandomValues.CreateBytes(10)
			},
			EncryptedDek = RandomValues.CreateBytes(10),
			FileNotes = new Dictionary<Guid, byte[]>
			{
				[fileId] = RandomValues.CreateBytes(10)
			},
			FolderId = folderId,
			FolderNotes = new Dictionary<Guid, byte[]>
			{
				[folderId] = RandomValues.CreateBytes(10)
			},
			UpdatedAt = default
		});

		// Assert
		await fileRepository
			.DidNotReceiveWithAnyArgs()
			.UpdatePropertiesAsync(default, default!, default);

		await folderRepository
			.DidNotReceiveWithAnyArgs()
			.UpdatePropertiesAsync(default, default!, default);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateProtectionAsync" />: the new contents, the notes and the wrapped key reach their rows, and
	/// what changes gets the date; a file note goes in only with the contents of its file.
	/// </summary>
	[Test]
	public async Task UpdateProtectionAsync_Writes_The_Contents_The_Notes_And_The_Key()
	{
		// Arrange
		using TestDatabase database = new();

		// The database keeps dates to the second.
		DateTime before = new(2020, 1, 2, 3, 4, 5);

		DateTime updatedAt = new(2021, 2, 3, 4, 5, 6);

		byte[] oldContents = RandomValues.CreateBytes(10);

		byte[] oldNote = RandomValues.CreateBytes(10);

		FolderEntity keeper = EntityFactory.CreateFolder(encryptedDek: RandomValues.CreateBytes(10), note: oldNote);

		FolderEntity subfolder = EntityFactory.CreateFolder(parentId: keeper.Id, note: oldNote);

		FileEntity noted = EntityFactory.CreateFile(contents: oldContents, parentId: keeper.Id, note: oldNote);

		FileEntity unnoted = EntityFactory.CreateFile(contents: oldContents, parentId: subfolder.Id, note: oldNote);

		FileEntity untouched = EntityFactory.CreateFile(contents: oldContents, parentId: subfolder.Id, note: oldNote);

		ExplorerItemBase[] entities = [keeper, subfolder, noted, unnoted, untouched];

		entities.ForEach(x => x.UpdatedAt = before);

		byte[] encryptedDek = RandomValues.CreateBytes(10);

		byte[] notedContents = RandomValues.CreateBytes(10);

		byte[] unnotedContents = RandomValues.CreateBytes(10);

		byte[] subfolderNote = RandomValues.CreateBytes(10);

		byte[] notedNote = RandomValues.CreateBytes(10);

		ProtectionUpdate update = new()
		{
			Contents = new Dictionary<Guid, byte[]>
			{
				[noted.Id] = notedContents,
				[unnoted.Id] = unnotedContents
			},
			EncryptedDek = encryptedDek,
			FileNotes = new Dictionary<Guid, byte[]>
			{
				[noted.Id] = notedNote,
				[untouched.Id] = RandomValues.CreateBytes(10)
			},
			FolderId = keeper.Id,
			FolderNotes = new Dictionary<Guid, byte[]>
			{
				[subfolder.Id] = subfolderNote
			},
			UpdatedAt = updatedAt
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(database.Context);

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

		DbAccess sut = mock.Create<DbAccess>();

		await sut.AddFoldersAsync([keeper, subfolder]);

		await sut.AddFilesAsync([noted, unnoted, untouched]);

		// Act
		await sut.UpdateProtectionAsync(update);

		// Assert
		FolderEntity[] folders = await sut.GetAllFoldersAsync();

		folders
			.Should()
			.BeEquivalentTo(new[]
			{
				new
				{
					EncryptedDek = (byte[]?)encryptedDek,
					Id = keeper.Id,
					Note = oldNote,
					UpdatedAt = before
				},
				new
				{
					EncryptedDek = (byte[]?)null,
					Id = subfolder.Id,
					Note = subfolderNote,
					UpdatedAt = updatedAt
				}
			});

		FileEntity[] files = await sut.GetAllFilesAsync(OptionalFileProperties.Contents);

		files
			.Should()
			.BeEquivalentTo(new[]
			{
				new
				{
					Contents = notedContents,
					Id = noted.Id,
					Note = notedNote,
					UpdatedAt = updatedAt
				},
				new
				{
					Contents = unnotedContents,
					Id = unnoted.Id,
					Note = oldNote,
					UpdatedAt = updatedAt
				},
				new
				{
					Contents = oldContents,
					Id = untouched.Id,
					Note = oldNote,
					UpdatedAt = before
				}
			});
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Wraps a synchronous sequence into an <see cref="IAsyncEnumerable{T}" /> for substitute setup.
	/// </summary>
	private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
	{
		await Task.CompletedTask;

		foreach (T item in items)
		{
			yield return item;
		}
	}
	#endregion
}
