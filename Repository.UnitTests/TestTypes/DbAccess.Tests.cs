using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using NSubstitute;
using Repository.DTO;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Services;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DbAccess.AddEntityAsync" />: creates a folder or file entity with the supplied parameters and saves changes.
	/// </summary>
	[Test]
	public async Task AddEntityAsync_Returns_Entity([Values] EntityType type)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFolderRepository folderRepository = Substitute.For<IFolderRepository>();

		IFileRepository fileRepository = Substitute.For<IFileRepository>();

		AddEntityParameters parameters = new()
		{
			EntityType = type,
			Index = TestUtils.CreateRandomIntFrom10To100(),
			Name = AppUtils.CreateRandomString(10),
			ParentId = Guid.NewGuid()
		};

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(folderRepository),
			TypedParameter.From(fileRepository));

		// Act
		ExplorerModelBase? entity = await sut.AddEntityAsync(parameters);

		// Assert
		entity
			.Should()
			.NotBeNull();

		entity.Id
			.Should()
			.NotBeEmpty();

		entity.EntityType
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

		if (type == EntityType.Folder)
		{
			entity
				.Should()
				.BeOfType<FolderModel>();

			await folderRepository
				.Received()
				.AddAsync(Arg.Any<FolderModel>());
		}
		else
		{
			entity
				.Should()
				.BeOfType<FileModel>();

			await fileRepository
				.Received()
				.AddAsync(Arg.Any<FileModel>());
		}
	}

	/// <summary>
	/// Test of <see cref="DbAccess.AddFilesAsync" />: adds the files via the repository, saves changes and returns true.
	/// </summary>
	[Test]
	public async Task AddFilesAsync_Adds_Files_To_Database()
	{
		// Arrange
		FileModel[] files = [.. TestUtils.CreateFiles(5)];

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
			.AddRangeAsync(Arg.Any<IEnumerable<FileModel>>());

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.AddFoldersAsync" />: adds the folders via the repository, saves changes and returns true.
	/// </summary>
	[Test]
	public async Task AddFoldersAsync_Adds_Folders_To_Database()
	{
		// Arrange
		FolderModel[] folders = [.. TestUtils.CreateFolders(5)];

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
			.AddRangeAsync(Arg.Any<IEnumerable<FolderModel>>());

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.AddHotkeysAsync" />: adds one hotkey per pair owned by the given file and saves changes.
	/// </summary>
	[Test]
	public async Task AddHotkeysAsync_Adds_Hotkeys_To_Database_And_Returns_Created_Models()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		CodeMaskPair[] pairs = [.. TestUtils.CreateCodeMaskPairs(5)];

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>(
			TypedParameter.From(dbConnection),
			TypedParameter.From(repository));

		// Act
		HotkeyModel[] result = await sut.AddHotkeysAsync(fileId, pairs);

		// Assert
		result
			.Should()
			.HaveCount(pairs.Length);

		result
			.Should()
			.OnlyContain(x => x.OwnerId == fileId);

		await repository
			.Received(pairs.Length)
			.AddAsync(Arg.Any<HotkeyModel>());

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.ClearDatabaseAsync" />: deletes the database and recreates it via migrations or creation depending on migration support.
	/// </summary>
	[Test]
	public async Task ClearDatabaseAsync_Recreates_Database([Values] bool useMigrations)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbConnection
				.HasMigrations(Arg.Any<Assembly>())
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
	/// Test of <see cref="DbAccess.ConnectAsync" />: migrates or creates the database depending on migration support.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Connects_To_Database([Values] bool useMigrations)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbConnection
				.HasMigrations(Arg.Any<Assembly>())
				.Returns(useMigrations);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.ConnectAsync();

		// Assert
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
	/// Test of <see cref="DbAccess.CountOfAsync" />: returns the count of entities matching the predicate.
	/// </summary>
	[Test]
	public async Task CountOfAsync_Returns_Count_Of_Matching_Entities()
	{
		// Arrange
		const int expectedCount = 7;

		IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.CountOfAsync(Arg.Any<Expression<Func<ExplorerModelBase, bool>>>())
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
	/// Test of <see cref="DbAccess.DeleteFileAsync" />: removes the file's hotkeys and the file itself, returning true.
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
	/// Test of <see cref="DbAccess.DeleteFolderAsync" />: removes an empty folder without touching file or hotkey repositories.
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
	/// Test of <see cref="DbAccess.DeleteFolderAsync" />: removes the whole subtree including nested folders, files and their hotkeys.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Removes_Folder_With_Nested_Folders_And_Files()
	{
		// Arrange
		Guid rootId = Guid.NewGuid();

		Guid[] subtreeIds = [rootId, Guid.NewGuid(), Guid.NewGuid()];

		Guid[] fileIds = [.. TestUtils.CreateGuids(4)];

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
	/// Test of <see cref="DbAccess.DeleteFolderAsync" />: returns false when no folder rows are removed.
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
	/// Test of <see cref="DbAccess.DeleteHotkeysAsync" />: removes all hotkeys owned by the given id and returns true.
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
	/// Test of <see cref="DbAccess.Dispose" />: calling it twice does not throw.
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
	/// Test of <see cref="DbAccess.GetAllFilesAsync" />: returns all files from the repository.
	/// </summary>
	[Test]
	public async Task GetAllFilesAsync_Returns_Files()
	{
		// Arrange
		FileModel[] expectedResult = [.. TestUtils.CreateFiles(100)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.GetAllAsync(OptionalFileProperty.None)
				.Returns(expectedResult);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		FileModel[] result = await sut.GetAllFilesAsync(OptionalFileProperty.None);

		// Assert
		result
			.Should()
			.BeEquivalentTo(expectedResult);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.GetAllFoldersAsync" />: returns all folders from the repository.
	/// </summary>
	[Test]
	public async Task GetAllFoldersAsync_Returns_Folders()
	{
		// Arrange
		FolderModel[] expectedResult = [.. TestUtils.CreateFolders(100)];

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
		FolderModel[] result = await sut.GetAllFoldersAsync();

		// Assert
		result
			.Should()
			.BeEquivalentTo(expectedResult);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.GetFileContentsAsync" />: returns a valid pair with the file's id and contents.
	/// </summary>
	[Test]
	public async Task GetFileContentsAsync_Returns_File_Contents()
	{
		// Arrange
		FileModel file = TestUtils.CreateFile();

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
		ContentsIsValidPair result = await sut.GetFileContentsAsync(file.Id);

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
	/// Test of <see cref="DbAccess.GetFilePropertiesAsync" />: returns the file's properties string.
	/// </summary>
	[Test]
	public async Task GetFilePropertiesAsync_Returns_File_Properties()
	{
		// Arrange
		FileModel file = TestUtils.CreateFile();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.GetPropertiesAsync(Arg.Any<Guid>())
				.Returns(file.Properties);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		string? result = await sut.GetFilePropertiesAsync(file.Id);

		// Assert
		result
			.Should()
			.Be(file.Properties);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.GetFilesContentsAsync" />: yields a valid contents pair for each requested identifier.
	/// </summary>
	[Test]
	public async Task GetFilesContentsAsync_Yields_Pair_For_Each_Identifier()
	{
		// Arrange
		FileModel[] files = [.. TestUtils.CreateFiles(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			foreach (FileModel file in files)
			{
				repository
					.GetContentsAsync(file.Id)
					.Returns(file.Contents);
			}

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		List<ContentsIsValidPair> results = [];

		await foreach (ContentsIsValidPair pair in sut.GetFilesContentsAsync(files.Select(x => x.Id)))
		{
			results.Add(pair);
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
	/// Test of <see cref="DbAccess.IsExistsAsync" />: returns true when an entity matches the id.
	/// </summary>
	[Test]
	public async Task IsExistsAsync_Returns_True_When_Entity_Exists()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.IsExistsAsync(Arg.Any<Expression<Func<ExplorerModelBase, bool>>>())
				.Returns(true);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.IsExistsAsync(id);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.UpdateFilePropertiesAsync(IDictionary{Guid, Action{UpdateSettersBuilder{FileModel}}[]}, System.Threading.CancellationToken)" />: returns false when the batch update affects no rows.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_False_When_Batch_Update_Affects_No_Rows()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, AppUtils.CreateRandomString(10))]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
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
	/// Test of <see cref="DbAccess.UpdateFilePropertiesAsync(Guid, Action{UpdateSettersBuilder{FileModel}}[], System.Threading.CancellationToken)" />: returns false when the file does not exist.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_False_When_File_Does_Not_Exist()
	{
		// Arrange
		Action<UpdateSettersBuilder<FileModel>>[] setters =
		[
			x => x.SetProperty(x => x.Name, AppUtils.CreateRandomString(10))
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFileRepository repository = Substitute.For<IFileRepository>();

			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>())
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
	/// Test of <see cref="DbAccess.UpdateFilePropertiesAsync(IDictionary{Guid, Action{UpdateSettersBuilder{FileModel}}[]}, System.Threading.CancellationToken)" />: returns true and forwards the updates when the batch update affects rows.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_True_When_Batch_Update_Affects_Any_Rows()
	{
		// Arrange
		Dictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates = new()
		{
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Name, AppUtils.CreateRandomString(10))],
			[Guid.NewGuid()] = [x => x.SetProperty(x => x.Index, TestUtils.CreateRandomIntFrom10To100())]
		};

		IFileRepository repository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(Arg.Any<IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]>>())
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
	/// Test of <see cref="DbAccess.UpdateFilePropertiesAsync(Guid, Action{UpdateSettersBuilder{FileModel}}[], System.Threading.CancellationToken)" />: returns true and forwards the setters when the file was updated.
	/// </summary>
	[Test]
	public async Task UpdateFilePropertiesAsync_Returns_True_When_File_Was_Updated()
	{
		// Arrange
		Guid fileId = Guid.NewGuid();

		Action<UpdateSettersBuilder<FileModel>>[] setters =
		[
			x => x.SetProperty(x => x.Name, AppUtils.CreateRandomString(10))
		];

		IFileRepository repository = Substitute.For<IFileRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FileModel>>[]>())
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
	/// Test of <see cref="DbAccess.UpdateFolderPropertiesAsync" />: returns false when the folder does not exist.
	/// </summary>
	[Test]
	public async Task UpdateFolderPropertiesAsync_Returns_False_When_Folder_Does_Not_Exist()
	{
		// Arrange
		Action<UpdateSettersBuilder<FolderModel>>[] setters =
		[
			x => x.SetProperty(x => x.Name, AppUtils.CreateRandomString(10))
		];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFolderRepository repository = Substitute.For<IFolderRepository>();

			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
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
	/// Test of <see cref="DbAccess.UpdateFolderPropertiesAsync" />: returns true and forwards the setters when the folder was updated.
	/// </summary>
	[Test]
	public async Task UpdateFolderPropertiesAsync_Returns_True_When_Folder_Was_Updated()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		Action<UpdateSettersBuilder<FolderModel>>[] setters =
		[
			x => x.SetProperty(x => x.Name, AppUtils.CreateRandomString(10))
		];

		IFolderRepository repository = Substitute.For<IFolderRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.UpdatePropertiesAsync(
					Arg.Any<Guid>(),
					Arg.Any<Action<UpdateSettersBuilder<FolderModel>>[]>())
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
