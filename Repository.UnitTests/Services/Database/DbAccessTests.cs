using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Repository.UnitTests.Fixtures;
using Shared.Common;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TestSupport;

namespace Repository.UnitTests.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
	/// <summary>
	/// <see cref="DbAccess.AddEntityAsync" />: creates a folder or file entity with the supplied parameters and saves changes.
	/// </summary>
	[Test]
	public async Task AddEntityAsync_Returns_Entity([Values] EntityKind type)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		AddEntityParameters parameters = new()
		{
			Index = TestData.CreateRandomIntFrom10To100(),
			Kind = type,
			Name = RandomString.Create(10),
			ParentId = Guid.NewGuid()
		};

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(folderRepository),
			TypedParameter.From(fileRepository));

		// Act
		ExplorerItemBase? entity = await sut.AddEntityAsync(parameters);

		// Assert
		entity
			.Should()
			.NotBeNull();

		entity.Id
			.Should()
			.NotBeEmpty();

		entity.Kind
			.Should()
			.Be(type);

		entity.Name
			.Should()
			.Be(parameters.Name);

		entity.Index
			.Should()
			.Be(parameters.Index);

		entity.ParentId
			.Should()
			.Be(parameters.ParentId);

		await dbConnection
			.Received()
			.SaveChangesAsync();

		if (type == EntityKind.Folder)
		{
			entity
				.Should()
				.BeOfType<FolderEntity>();

			await folderRepository
				.Received()
				.AddAsync(Arg.Any<FolderEntity>());
		}
		else
		{
			entity
				.Should()
				.BeOfType<FileEntity>();

			await fileRepository
				.Received()
				.AddAsync(Arg.Any<FileEntity>());
		}
	}

	/// <summary>
	/// <see cref="DbAccess.AddFilesAsync" />: adds the files via the repository, saves changes and returns true.
	/// </summary>
	[Test]
	public async Task AddFilesAsync_Adds_Files_To_Database()
	{
		// Arrange
		FileEntity[] files = [.. TestData.CreateFiles(5)];

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFileRepository repository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(repository));

		// Act
		bool result = await sut.AddFilesAsync(files);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.AddRangeAsync(Arg.Any<IEnumerable<FileEntity>>());

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// <see cref="DbAccess.AddFoldersAsync" />: adds the folders via the repository, saves changes and returns true.
	/// </summary>
	[Test]
	public async Task AddFoldersAsync_Adds_Folders_To_Database()
	{
		// Arrange
		FolderEntity[] folders = [.. TestData.CreateFolders(5)];

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFolderRepository repository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(repository),
			TypedParameter.From(dbConnection));

		// Act
		bool result = await sut.AddFoldersAsync(folders);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.AddRangeAsync(Arg.Any<IEnumerable<FolderEntity>>());

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// <see cref="DbAccess.AddHotkeysAsync" />: adds one hotkey per key stroke owned by the given file and saves changes.
	/// </summary>
	[Test]
	public async Task AddHotkeysAsync_Adds_Hotkeys_To_Database_And_Returns_Created_Models()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		KeyStroke[] keyStrokes = [.. TestData.CreateKeyStrokes(5)];

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(repository));

		// Act
		HotkeyEntity[] result = await sut.AddHotkeysAsync(fileId, keyStrokes);

		// Assert
		result
			.Should()
			.HaveCount(keyStrokes.Length);

		result
			.Should()
			.OnlyContain(x => x.OwnerId == fileId);

		await repository
			.Received(keyStrokes.Length)
			.AddAsync(Arg.Any<HotkeyEntity>());

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// <see cref="DbAccess.BackupDatabaseAsync" />: the copy appears in the folder of the copies and is gone once released.
	/// </summary>
	[Test]
	public async Task BackupDatabaseAsync_Creates_A_Copy_That_Lives_Until_It_Is_Released()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");
		}

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		dbContextService
			.GetDbFilePath()
			.Returns(file.FilePath);

		IFileSystem fileSystem = new FileSystem(Substitute.For<IJsonSerializer>());

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbContextService),
			TypedParameter.From(fileSystem));

		// Act
		DatabaseBackup? backup = await sut.BackupDatabaseAsync();

		// Assert
		backup
			.Should()
			.NotBeNull();

		Path
			.GetDirectoryName(backup.FilePath)
			.Should()
			.Be(DatabaseBackup.GetDirectoryPath(file.FilePath));

		File
			.Exists(backup.FilePath)
			.Should()
			.BeTrue();

		backup.Dispose();

		File
			.Exists(backup.FilePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.ClearDatabaseAsync" />: deletes the database and recreates it via migrations or creation depending on migration support.
	/// </summary>
	[Test]
	public async Task ClearDatabaseAsync_Recreates_Database([Values] bool useMigrations)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbConnection
				.HasMigrations()
				.Returns(useMigrations);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.ClearDatabaseAsync();

		// Assert
		result
			.Should()
			.BeTrue();

		dbConnection
			.Received()
			.EnsureDeleted();

		if (useMigrations)
		{
			dbConnection
				.Received()
				.Migrate();
		}
		else
		{
			dbConnection
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
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbConnection
				.HasMigrations()
				.Returns(useMigrations);

			builder.RegisterInstance(dbConnection);
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
			await dbConnection
				.Received()
				.MigrateAsync();
		}
		else
		{
			await dbConnection
				.Received()
				.EnsureCreatedAsync();
		}
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a migration applied by a newer version is seen before the schema is touched.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Reports_A_Database_From_A_Newer_Version()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");
		}

		const string known = "20260907183944_InitialCreate";

		IDbContextService dbConnection = CreateExistingDatabase(file);

		dbConnection
			.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>())
			.Returns([known, "20991231235959_FromTheFuture"]);

		dbConnection
			.GetKnownMigrations()
			.Returns([known]);

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From<IFileSystem>(new FileSystem(Substitute.For<IJsonSerializer>())));

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.SchemaTooNew);

		await dbConnection
			.DidNotReceive()
			.MigrateAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a file that is not a database is not migrated.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Reports_A_File_It_Cannot_Open()
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		dbConnection
			.HasMigrations()
			.Returns(true);

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.FileExists(Arg.Any<string>())
			.Returns(true);

		fileSystem
			.OpenRead(Arg.Any<string>())
			.Returns(_ => new MemoryStream(new byte[32]));

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(fileSystem));

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.FileUnreadable);

		await dbConnection
			.DidNotReceive()
			.MigrateAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a migration that fails on a readable database is about its schema.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Reports_A_Schema_It_Cannot_Update()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");
		}

		IDbContextService dbConnection = CreateExistingDatabase(file);

		dbConnection
			.MigrateAsync(Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException(@"Table ""Payloads"" already exists"));

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From<IFileSystem>(new FileSystem(Substitute.For<IJsonSerializer>())));

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.SchemaTooOld);
	}

	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a database that cannot be created is reported as unreadable.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Reports_An_Unusable_Database()
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbConnection
				.EnsureCreatedAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(dbConnection);
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
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		dbConnection
			.EnsureCreatedAsync(Arg.Any<CancellationToken>())
			.ThrowsAsync(new InvalidOperationException());

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(fileRepository),
			TypedParameter.From(folderRepository));

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

		(await sut.AddEntityAsync(new()
		{
			Index = 0,
			Kind = EntityKind.Folder,
			Name = RandomString.Create(10),
			ParentId = Guid.NewGuid()
		}))
			.Should()
			.BeNull();

		(await sut.AddFilesAsync([]))
			.Should()
			.BeFalse();

		(await sut.AddHotkeysAsync(Guid.NewGuid(), []))
			.Should()
			.BeEmpty();

		(await sut.ClearDatabaseAsync())
			.Should()
			.BeFalse();

		(await sut.DeleteFileAsync(Guid.NewGuid()))
			.Should()
			.BeFalse();

		await dbConnection
			.DidNotReceive()
			.SaveChangesAsync(Arg.Any<CancellationToken>());

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
		IDbMaintenance dbMaintenance = Substitute.For<IDbMaintenance>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
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

		IExplorerItemRepository repository = Substitute.For<IExplorerItemRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
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
	/// <see cref="DbAccess.DeleteFileAsync" />: removes the file's hotkeys and the file itself, returning true.
	/// </summary>
	[Test]
	public async Task DeleteFileAsync_Deletes_File_From_Database()
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
		bool result = await sut.DeleteFileAsync(default);

		// Assert
		result
			.Should()
			.BeTrue();

		await hotkeysRepository
			.Received()
			.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>());

		await fileRepository
			.Received()
			.RemoveAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteFolderAsync" />: removes an empty folder without touching file or hotkey repositories.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Deletes_Folder_From_Database()
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
		bool result = await sut.DeleteFolderAsync(folderId);

		// Assert
		result
			.Should()
			.BeTrue();

		await folderRepository
			.Received()
			.RemoveRangeByIdsAsync(Arg.Any<Guid[]>());

		await hotkeysRepository
			.DidNotReceive()
			.RemoveRangeByOwnerIdsAsync(Arg.Any<Guid[]>());

		await fileRepository
			.DidNotReceive()
			.RemoveRangeByIdsAsync(Arg.Any<Guid[]>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteFolderAsync" />: removes the whole subtree including nested folders, files and their hotkeys.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Removes_Folder_With_Nested_Folders_And_Files()
	{
		// Arrange
		Guid rootId = Guid.NewGuid();

		Guid[] subtreeIds = [rootId, Guid.NewGuid(), Guid.NewGuid()];

		Guid[] fileIds = [.. TestData.CreateGuids(4)];

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
		bool result = await sut.DeleteFolderAsync(rootId);

		// Assert
		result
			.Should()
			.BeTrue();

		await hotkeysRepository
			.Received()
			.RemoveRangeByOwnerIdsAsync(fileIds);

		await fileRepository
			.Received()
			.RemoveRangeByIdsAsync(fileIds);

		await folderRepository
			.Received()
			.RemoveRangeByIdsAsync(Arg.Is<Guid[]>(x => x.SequenceEqual(subtreeIds)));
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteFolderAsync" />: returns false when no folder rows are removed.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Returns_False_When_Folder_Does_Not_Exist()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
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
		bool result = await sut.DeleteFolderAsync(folderId);

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
			.Received()
			.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>());
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
	/// <see cref="DbAccess.GetAllFilesAsync" />: returns all files from the repository.
	/// </summary>
	[Test]
	public async Task GetAllFilesAsync_Returns_Files()
	{
		// Arrange
		FileEntity[] expectedResult = [.. TestData.CreateFiles(100)];

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
		FolderEntity[] expectedResult = [.. TestData.CreateFolders(100)];

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
		FileEntity file = TestData.CreateFile();

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
	/// <see cref="DbAccess.GetFileEditorStateAsync" />: returns the stored editor state of the file.
	/// </summary>
	[Test]
	public async Task GetFileEditorStateAsync_Returns_The_Editor_State()
	{
		// Arrange
		FileEntity file = TestData.CreateFile();

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
	/// <see cref="DbAccess.GetFileContentsRangeAsync" />: yields valid contents for each requested identifier.
	/// </summary>
	[Test]
	public async Task GetFileContentsRangeAsync_Yields_Pair_For_Each_Identifier()
	{
		// Arrange
		FileEntity[] files = [.. TestData.CreateFiles(3)];

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
	/// <see cref="DbAccess.UpdateFileAndFolderPropertiesAsync" />: returns false when a write inside the transaction throws.
	/// </summary>
	[Test]
	public async Task UpdateFileAndFolderPropertiesAsync_Returns_False_When_A_Write_Throws()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> fileUpdates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, RandomString.Create(10))]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository fileRepository = Substitute.For<IFileRepository>();

			fileRepository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>(),
					Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException());

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(CreateContextServiceRunningTheTransaction());
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFileAndFolderPropertiesAsync(fileUpdates, new Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>());

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileAndFolderPropertiesAsync" />: applies the updates of both entity types in one transaction.
	/// </summary>
	[Test]
	public async Task UpdateFileAndFolderPropertiesAsync_Returns_True_And_Forwards_Every_Update()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		Guid folderId = Guid.NewGuid();

		Action<UpdateSettersBuilder<FileEntity>>[] fileSetters =
		[
			x => x.SetProperty(x => x.Name, RandomString.Create(10))
		];

		Action<UpdateSettersBuilder<FolderEntity>>[] folderSetters =
		[
			x => x.SetProperty(x => x.Name, RandomString.Create(10))
		];

		Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> fileUpdates = new()
		{
			[fileId] = fileSetters
		};

		Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> folderUpdates = new()
		{
			[folderId] = folderSetters
		};

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(folderRepository);

			builder.RegisterInstance(CreateContextServiceRunningTheTransaction());
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFileAndFolderPropertiesAsync(fileUpdates, folderUpdates);

		// Assert
		result
			.Should()
			.BeTrue();

		await fileRepository
			.Received(1)
			.UpdatePropertiesAsync(fileId, fileSetters, Arg.Any<CancellationToken>());

		await folderRepository
			.Received(1)
			.UpdatePropertiesAsync(folderId, folderSetters, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileAndFolderPropertiesAsync" />: an empty set of updates is not a failure.
	/// </summary>
	[Test]
	public async Task UpdateFileAndFolderPropertiesAsync_Returns_True_When_There_Is_Nothing_To_Update()
	{
		// Arrange
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(CreateContextServiceRunningTheTransaction()));

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFileAndFolderPropertiesAsync(
			new Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>(),
			new Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>());

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFilePropertiesAsync(IDictionary{Guid, Action{UpdateSettersBuilder{FileEntity}}[]}, System.Threading.CancellationToken)" />: returns false when the batch update affects no rows.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_False_When_Batch_Update_Affects_No_Rows()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> updates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, RandomString.Create(10))]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>>())
				.Returns(0);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFilePropertiesAsync(updates);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFilePropertiesAsync(Guid, Action{UpdateSettersBuilder{FileEntity}}[], System.Threading.CancellationToken)" />: returns false when the file does not exist.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_False_When_File_Does_Not_Exist()
	{
		// Arrange
		Action<UpdateSettersBuilder<FileEntity>>[] setters =
		[
			x => x.SetProperty(x => x.Name, RandomString.Create(10))
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>())
				.Returns(0);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFilePropertiesAsync(Guid.NewGuid(), setters);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFilePropertiesAsync(IDictionary{Guid, Action{UpdateSettersBuilder{FileEntity}}[]}, System.Threading.CancellationToken)" />: returns true and forwards the updates when the batch update affects rows.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_True_When_Batch_Update_Affects_Any_Rows()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> updates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, RandomString.Create(10))],
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Index, TestData.CreateRandomIntFrom10To100())]
		};

		IFileRepository repository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>>())
				.Returns(updates.Count);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFilePropertiesAsync(updates);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.UpdatePropertiesAsync(updates);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFilePropertiesAsync(Guid, Action{UpdateSettersBuilder{FileEntity}}[], System.Threading.CancellationToken)" />: returns true and forwards the setters when the file was updated.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_True_When_File_Was_Updated()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		Action<UpdateSettersBuilder<FileEntity>>[] setters =
		[
			x => x.SetProperty(x => x.Name, RandomString.Create(10))
		];

		IFileRepository repository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileEntity>>[]>())
				.Returns(1);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFilePropertiesAsync(fileId, setters);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.UpdatePropertiesAsync(fileId, setters);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFolderPropertiesAsync(IDictionary{Guid, Action{UpdateSettersBuilder{FolderEntity}}[]}, System.Threading.CancellationToken)" />: returns false when the batch update affects no rows.
	/// </summary>
	[Test]
	public async Task UpdateFolderPropertiesAsync_Returns_False_When_Batch_Update_Affects_No_Rows()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> updates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, RandomString.Create(10))]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderRepository repository = Substitute.For<IFolderRepository>();

			repository
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>>())
				.Returns(0);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFolderPropertiesAsync(updates);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFolderPropertiesAsync(Guid, Action{UpdateSettersBuilder{FolderEntity}}[], System.Threading.CancellationToken)" />: returns false when the folder does not exist.
	/// </summary>
	[Test]
	public async Task UpdateFolderPropertiesAsync_Returns_False_When_Folder_Does_Not_Exist()
	{
		// Arrange
		Action<UpdateSettersBuilder<FolderEntity>>[] setters =
		[
			x => x.SetProperty(x => x.Name, RandomString.Create(10))
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderRepository repository = Substitute.For<IFolderRepository>();

			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>())
				.Returns(0);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFolderPropertiesAsync(Guid.NewGuid(), setters);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFolderPropertiesAsync(IDictionary{Guid, Action{UpdateSettersBuilder{FolderEntity}}[]}, System.Threading.CancellationToken)" />: returns true and forwards the updates when the batch update affects rows.
	/// </summary>
	[Test]
	public async Task UpdateFolderPropertiesAsync_Returns_True_When_Batch_Update_Affects_Any_Rows()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> updates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, RandomString.Create(10))],
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Index, TestData.CreateRandomIntFrom10To100())]
		};

		IFolderRepository repository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>>())
				.Returns(updates.Count);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFolderPropertiesAsync(updates);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.UpdatePropertiesAsync(updates);
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFolderPropertiesAsync(Guid, Action{UpdateSettersBuilder{FolderEntity}}[], System.Threading.CancellationToken)" />: returns true and forwards the setters when the folder was updated.
	/// </summary>
	[Test]
	public async Task UpdateFolderPropertiesAsync_Returns_True_When_Folder_Was_Updated()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		Action<UpdateSettersBuilder<FolderEntity>>[] setters =
		[
			x => x.SetProperty(x => x.Name, RandomString.Create(10))
		];

		IFolderRepository repository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FolderEntity>>[]>())
				.Returns(1);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdateFolderPropertiesAsync(folderId, setters);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.UpdatePropertiesAsync(folderId, setters);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// A substitute of <see cref="IDbContextService" /> that runs the body of the transaction.
	/// </summary>
	private static IDbContextService CreateContextServiceRunningTheTransaction()
	{
		IDbContextService contextService = Substitute.For<IDbContextService>();

		contextService
			.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
			.Returns(x => x.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

		return contextService;
	}

	/// <summary>
	/// A substitute of <see cref="IDbContextService" /> that opens the database of <paramref name="file" />
	/// and reports it as migrated by this version.
	/// </summary>
	private static IDbContextService CreateExistingDatabase(TempSqliteFile file)
	{
		IDbContextService contextService = Substitute.For<IDbContextService>();

		contextService
			.CanConnectAsync(Arg.Any<CancellationToken>())
			.Returns(true);

		contextService
			.GetDbFilePath()
			.Returns(file.FilePath);

		contextService
			.HasMigrations()
			.Returns(true);

		return contextService;
	}

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
