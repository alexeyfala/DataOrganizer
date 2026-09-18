using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Dto;
using Repository.Enums;
using Repository.Exceptions;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Common;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Dto;
using TestSupport.Models;

namespace Repository.UnitTests.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Data
	/// <summary>
	/// Path the database file is taken to live at.
	/// </summary>
	private const string DatabaseFilePath = @"C:\Database\DataOrganizer.sqlite";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DbAccess.AddEntityAsync" />: creates a folder or file entity with the supplied parameters and saves changes.
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
	/// <see cref="DbAccess.AddFilesAsync" />: adds the files via the repository and saves changes.
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
	}

	/// <summary>
	/// <see cref="DbAccess.AddFoldersAsync" />: adds the folders via the repository and saves changes.
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
	}

	/// <summary>
	/// <see cref="DbAccess.AddHotkeysAsync" />: adds one hotkey per key stroke owned by the given file and saves changes.
	/// </summary>
	[Test]
	public async Task AddHotkeysAsync_Adds_Hotkeys_To_Database_And_Returns_Created_Models()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		KeyStroke[] keyStrokes = [.. KeyStrokeFactory.CreateKeyStrokes(5)];

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

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

		await dbContextService
			.Received(1)
			.SaveChangesAsync();
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

		await AssertRefusedAsync(() => sut.AddHotkeysAsync(Guid.NewGuid(), []));

		await AssertRefusedAsync(() => sut.ClearDatabaseAsync());

		await AssertRefusedAsync(() => sut.DeleteFileAsync(Guid.NewGuid()));

		await AssertRefusedAsync(() => sut.DeleteFolderAsync(Guid.NewGuid()));

		await AssertRefusedAsync(() => sut.DeleteHotkeysAsync(Guid.NewGuid()));

		await AssertRefusedAsync(() => sut.RestoreFromBackupAsync(RandomString.Create(10)));

		await AssertRefusedAsync(() => sut.UpdateFileAndFolderPropertiesAsync(
			new Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>(),
			new Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>()));

		await AssertRefusedAsync(() => sut.UpdateFilePropertiesAsync(Guid.NewGuid(), []));

		await AssertRefusedAsync(() => sut.UpdateFilePropertiesAsync(
			new Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>()));

		await AssertRefusedAsync(() => sut.UpdateFolderPropertiesAsync(Guid.NewGuid(), []));

		await AssertRefusedAsync(() => sut.UpdateFolderPropertiesAsync(
			new Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>()));

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
				.Returns(DatabaseFilePath);

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
			.Received(1)
			.RemoveRangeByOwnerIdAsync(Arg.Any<Guid>());

		await fileRepository
			.Received(1)
			.RemoveAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// <see cref="DbAccess.DeleteFileAsync" />: returns false when no file rows are removed.
	/// </summary>
	[Test]
	public async Task DeleteFileAsync_Returns_False_When_File_Does_Not_Exist()
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
		bool result = await sut.DeleteFileAsync(Guid.NewGuid());

		// Assert
		result
			.Should()
			.BeFalse();
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
	/// <see cref="DbAccess.DeleteFolderAsync" />: removes the whole subtree including nested folders, files and their hotkeys.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Removes_Folder_With_Nested_Folders_And_Files()
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
		bool result = await sut.DeleteFolderAsync(rootId);

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
	/// <see cref="DbAccess.DeleteFolderAsync" />: returns false when no folder rows are removed.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Returns_False_When_Folder_Does_Not_Exist()
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
	/// <see cref="DbAccess.UpdateFileAndFolderPropertiesAsync" />: an empty set of updates is not a failure.
	/// </summary>
	[Test]
	public async Task UpdateFileAndFolderPropertiesAsync_Accepts_An_Empty_Set_Of_Updates()
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
		Func<Task> act = () => sut.UpdateFileAndFolderPropertiesAsync(
			new Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]>(),
			new Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>());

		// Assert
		await act
			.Should()
			.NotThrowAsync();
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileAndFolderPropertiesAsync" />: applies the updates of both entity types in one transaction.
	/// </summary>
	[Test]
	public async Task UpdateFileAndFolderPropertiesAsync_Forwards_Every_Update()
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
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			// The stub runs the body of the transaction instead of a database.
			dbContextService
				.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task>>(), Arg.Any<CancellationToken>())
				.Returns(x => x.Arg<Func<CancellationToken, Task>>()(CancellationToken.None));

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileRepository);

			builder.RegisterInstance(folderRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.UpdateFileAndFolderPropertiesAsync(fileUpdates, folderUpdates);

		// Assert
		await fileRepository
			.Received(1)
			.UpdatePropertiesAsync(fileId, fileSetters, Arg.Any<CancellationToken>());

		await folderRepository
			.Received(1)
			.UpdatePropertiesAsync(folderId, folderSetters, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="DbAccess.UpdateFileAndFolderPropertiesAsync" />: a write that failed inside the transaction
	/// reaches the caller.
	/// </summary>
	[Test]
	public async Task UpdateFileAndFolderPropertiesAsync_Lets_A_Failed_Write_Out()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> fileUpdates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, RandomString.Create(10))]
		};

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
		Func<Task> act = () => sut.UpdateFileAndFolderPropertiesAsync(
			fileUpdates,
			new Dictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]>());

		// Assert
		await act
			.Should()
			.ThrowAsync<InvalidOperationException>();
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
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Index, RandomValues.CreateIntFrom10To100())]
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
			.Received(1)
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
			.Received(1)
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
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Index, RandomValues.CreateIntFrom10To100())]
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
			.Received(1)
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
			.Received(1)
			.UpdatePropertiesAsync(folderId, setters);
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
