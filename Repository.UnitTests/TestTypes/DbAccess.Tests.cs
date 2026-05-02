using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using NSubstitute;
using Repository.DTO;
using Repository.Interfaces;
using Repository.Services;
using Shared.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DbAccess.AddEntityAsync" />.
	/// </summary>
	[Test]
	public async Task AddEntityAsync_Returns_Entity([Values] EntityType type)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFoldersRepository foldersRepository = Substitute.For<IFoldersRepository>();

		IFilesRepository filesRepository = Substitute.For<IFilesRepository>();

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
			TypedParameter.From(foldersRepository),
			TypedParameter.From(filesRepository));

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

			await foldersRepository
				.Received()
				.AddAsync(Arg.Any<FolderModel>());
		}
		else
		{
			entity
				.Should()
				.BeOfType<FileModel>();

			await filesRepository
				.Received()
				.AddAsync(Arg.Any<FileModel>());
		}
	}

	/// <summary>
	/// Test of <see cref="DbAccess.AddFilesAsync" />.
	/// </summary>
	[Test]
	public async Task AddFilesAsync_Adds_Files_To_Database()
	{
		// Arrange
		FileModel[] files = [.. TestUtils.CreateFiles(5)];

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFilesRepository repository = Substitute.For<IFilesRepository>();

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
	/// Test of <see cref="DbAccess.AddFoldersAsync" />.
	/// </summary>
	[Test]
	public async Task AddFoldersAsync_Adds_Folders_To_Database()
	{
		// Arrange
		FolderModel[] folders = [.. TestUtils.CreateFolders(5)];

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		IFoldersRepository repository = Substitute.For<IFoldersRepository>();

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
	/// Test of <see cref="DbAccess.AddHotkeysAsync" />.
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
	/// Test of <see cref="DbAccess.ClearDatabaseAsync" />.
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
	/// Test of <see cref="DbAccess.ConnectAsync" />.
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
	/// Test of <see cref="DbAccess.CountOfAsync" />.
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
	/// Test of <see cref="DbAccess.DeleteFileAsync" />.
	/// </summary>
	[Test]
	public async Task DeleteFileAsync_Deletes_File_From_Database()
	{
		// Arrange
		IHotkeysRepository hotkeysRepository = Substitute.For<IHotkeysRepository>();

		IFilesRepository filesRepository = Substitute.For<IFilesRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			filesRepository
				.RemoveAsync(Arg.Any<Guid>())
				.Returns(1);

			builder.RegisterInstance(filesRepository);

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

		await filesRepository
			.Received()
			.RemoveAsync(Arg.Any<Guid>());
	}

	/// <summary>
	/// Test of <see cref="DbAccess.DeleteFolderAsync" />.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Deletes_Folder_From_Database()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		Guid[] subtreeIds = [folderId];

		IFoldersRepository foldersRepository = Substitute.For<IFoldersRepository>();

		IFilesRepository filesRepository = Substitute.For<IFilesRepository>();

		IHotkeysRepository hotkeysRepository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			foldersRepository
				.GetFolderSubtreeIdsAsync(folderId)
				.Returns(ToAsyncEnumerable(subtreeIds));

			filesRepository
				.GetFileIdsAsync(Arg.Any<Guid[]>())
				.Returns([]);

			foldersRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(subtreeIds.Length);

			builder.RegisterInstance(foldersRepository);

			builder.RegisterInstance(filesRepository);

			builder.RegisterInstance(hotkeysRepository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteFolderAsync(folderId);

		// Assert
		result
			.Should()
			.BeTrue();

		await foldersRepository
			.Received()
			.RemoveRangeByIdsAsync(Arg.Any<Guid[]>());

		await hotkeysRepository
			.DidNotReceive()
			.RemoveRangeByOwnerIdsAsync(Arg.Any<Guid[]>());

		await filesRepository
			.DidNotReceive()
			.RemoveRangeByIdsAsync(Arg.Any<Guid[]>());
	}

	/// <summary>
	/// Test of <see cref="DbAccess.DeleteFolderAsync" />.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Removes_Folder_With_Nested_Folders_And_Files()
	{
		// Arrange
		Guid rootId = Guid.NewGuid();

		Guid[] subtreeIds = [rootId, Guid.NewGuid(), Guid.NewGuid()];

		Guid[] fileIds = [.. TestUtils.CreateGuids(4)];

		IFoldersRepository foldersRepository = Substitute.For<IFoldersRepository>();

		IFilesRepository filesRepository = Substitute.For<IFilesRepository>();

		IHotkeysRepository hotkeysRepository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			foldersRepository
				.GetFolderSubtreeIdsAsync(rootId)
				.Returns(ToAsyncEnumerable(subtreeIds));

			filesRepository
				.GetFileIdsAsync(Arg.Any<Guid[]>())
				.Returns(fileIds);

			hotkeysRepository
				.RemoveRangeByOwnerIdsAsync(Arg.Any<Guid[]>())
				.Returns(fileIds.Length);

			filesRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(fileIds.Length);

			foldersRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(subtreeIds.Length);

			builder.RegisterInstance(foldersRepository);

			builder.RegisterInstance(filesRepository);

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

		await filesRepository
			.Received()
			.RemoveRangeByIdsAsync(fileIds);

		await foldersRepository
			.Received()
			.RemoveRangeByIdsAsync(Arg.Is<Guid[]>(x => x.SequenceEqual(subtreeIds)));
	}

	/// <summary>
	/// Test of <see cref="DbAccess.DeleteFolderAsync" />.
	/// </summary>
	[Test]
	public async Task DeleteFolderAsync_Returns_False_When_Folder_Does_Not_Exist()
	{
		// Arrange
		Guid folderId = Guid.NewGuid();

		IFoldersRepository foldersRepository = Substitute.For<IFoldersRepository>();

		IFilesRepository filesRepository = Substitute.For<IFilesRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			foldersRepository
				.GetFolderSubtreeIdsAsync(folderId)
				.Returns(ToAsyncEnumerable<Guid>([]));

			filesRepository
				.GetFileIdsAsync(Arg.Any<Guid[]>())
				.Returns([]);

			foldersRepository
				.RemoveRangeByIdsAsync(Arg.Any<Guid[]>())
				.Returns(0);

			builder.RegisterInstance(foldersRepository);

			builder.RegisterInstance(filesRepository);
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
	/// Test of <see cref="DbAccess.DeleteHotkeysAsync" />.
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
	/// Test of <see cref="DbAccess.Dispose" />.
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
	/// Test of <see cref="DbAccess.GetAllFilesAsync" />.
	/// </summary>
	[Test]
	public async Task GetAllFilesAsync_Returns_Files()
	{
		// Arrange
		FileModel[] expectedResult = [.. TestUtils.CreateFiles(100)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFilesRepository repository = Substitute.For<IFilesRepository>();

			repository
				.GetAllAsync()
				.Returns(expectedResult);

			builder.RegisterInstance(repository);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		FileModel[] result = await sut.GetAllFilesAsync();

		// Assert
		result
			.Should()
			.BeEquivalentTo(expectedResult);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.GetAllFoldersAsync" />.
	/// </summary>
	[Test]
	public async Task GetAllFoldersAsync_Returns_Folders()
	{
		// Arrange
		FolderModel[] expectedResult = [.. TestUtils.CreateFolders(100)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFoldersRepository repository = Substitute.For<IFoldersRepository>();

			repository
				.GetAllAsync(Arg.Any<bool>())
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
	/// Test of <see cref="DbAccess.GetFileContentsAsync" />.
	/// </summary>
	[Test]
	public async Task GetFileContentsAsync_Returns_File_Contents()
	{
		// Arrange
		FileModel file = TestUtils.CreateFile();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFilesRepository repository = Substitute.For<IFilesRepository>();

			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>())
				.Returns(file);

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
	/// Test of <see cref="DbAccess.GetFilePropertiesAsync" />.
	/// </summary>
	[Test]
	public async Task GetFilePropertiesAsync_Returns_File_Properties()
	{
		// Arrange
		FileModel file = TestUtils.CreateFile();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFilesRepository repository = Substitute.For<IFilesRepository>();

			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>())
				.Returns(file);

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
	/// Test of <see cref="DbAccess.GetFilesContentsAsync" />.
	/// </summary>
	[Test]
	public async Task GetFilesContentsAsync_Yields_Pair_For_Each_Identifier()
	{
		// Arrange
		FileModel[] files = [.. TestUtils.CreateFiles(3)];

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IFilesRepository repository = Substitute.For<IFilesRepository>();

			foreach (FileModel file in files)
			{
				repository
					.FirstOrDefaultAsync(file.Id)
					.Returns(file);
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
	/// Test of <see cref="DbAccess.IsExistsAsync" />.
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
	/// Test of <see cref="DbAccess.UpdatePropertiesAsync(Guid, CancellationToken, PropertyNameValuePair[])" />.
	/// </summary>
	[Test]
	public async Task UpdatePropertiesAsync_By_Id_Updates_Properties_Of_Entity_In_Database()
	{
		// Arrange
		FileModel entity = TestUtils.CreateFile();

		string newName = AppUtils.CreateRandomString(10);

		int newIndex = TestUtils.CreateRandomInt(1, 100);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>(), Arg.Any<bool>())
				.Returns(entity);

			IDbContextService dbConnection = Substitute.For<IDbContextService>();

			dbConnection
				.SaveChangesAsync()
				.Returns(1);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		PropertyNameValuePair[] properties =
		[
			new(nameof(ExplorerModelBase.Name), newName),
			new(nameof(ExplorerModelBase.Index), newIndex)
		];

		// Act
		bool result = await sut.UpdatePropertiesAsync(entity.Id, default, properties);

		// Assert
		result
			.Should()
			.BeTrue();

		entity.Name
			.Should()
			.Be(newName);

		entity.Index
			.Should()
			.Be(newIndex);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.UpdatePropertiesAsync(IDictionary{Guid, PropertyNameValuePair[]}, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task UpdatePropertiesAsync_By_Relations_Updates_Properties_Of_Multiple_Entities()
	{
		// Arrange
		FileModel firstEntity = TestUtils.CreateFile();

		FileModel secondEntity = TestUtils.CreateFile();

		string firstName = AppUtils.CreateRandomString(10);

		string secondName = AppUtils.CreateRandomString(10);

		Dictionary<Guid, PropertyNameValuePair[]> relations = new()
		{
			[firstEntity.Id] = [new(nameof(ExplorerModelBase.Name), firstName)],
			[secondEntity.Id] = [new(nameof(ExplorerModelBase.Name), secondName)]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.FirstOrDefaultAsync(firstEntity.Id, Arg.Any<bool>())
				.Returns(firstEntity);

			repository
				.FirstOrDefaultAsync(secondEntity.Id, Arg.Any<bool>())
				.Returns(secondEntity);

			IDbContextService dbConnection = Substitute.For<IDbContextService>();

			dbConnection
				.SaveChangesAsync()
				.Returns(2);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdatePropertiesAsync(relations);

		// Assert
		result
			.Should()
			.BeTrue();

		firstEntity.Name
			.Should()
			.Be(firstName);

		secondEntity.Name
			.Should()
			.Be(secondName);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.UpdatePropertiesAsync{T}(T, CancellationToken, string[])" />.
	/// </summary>
	[Test]
	public async Task UpdatePropertiesAsync_Updates_Properties_Of_Entity_In_Database()
	{
		// Arrange
		FileModel entity = TestUtils.CreateFile();

		IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>(), Arg.Any<bool>())
				.Returns(entity);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.UpdatePropertiesAsync(
			dto,
			default,
			nameof(ExplorerModelBaseDto.CreatedDate),
			nameof(ExplorerModelBaseDto.Index),
			nameof(ExplorerModelBaseDto.IsSelected),
			nameof(ExplorerModelBaseDto.Name),
			nameof(ExplorerModelBaseDto.UpdatedDate));

		// Assert
		entity.CreatedDate
			.Should()
			.Be(dto.CreatedDate);

		entity.Index
			.Should()
			.Be(dto.Index);

		entity.IsSelected
			.Should()
			.Be(dto.IsSelected);

		entity.Name
			.Should()
			.Be(dto.Name);

		entity.UpdatedDate
			.Should()
			.Be(dto.UpdatedDate);

		await dbConnection
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.UpdatePropertyAsync{T}(Guid, string, T, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task UpdatePropertyAsync_Updates_Property_Of_Entity_In_Database()
	{
		// Arrange
		FileModel entity = TestUtils.CreateFile();

		string newName = AppUtils.CreateRandomString(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>(), Arg.Any<bool>())
				.Returns(entity);

			IDbContextService dbConnection = Substitute.For<IDbContextService>();

			dbConnection
				.SaveChangesAsync()
				.Returns(1);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdatePropertyAsync(
			default(Guid),
			nameof(ExplorerModelBase.Name),
			newName);

		// Assert
		result
			.Should()
			.BeTrue();

		entity.Name
			.Should()
			.Be(newName);
	}

	/// <summary>
	/// Test of <see cref="DbAccess.UpdatePropertyAsync{T}(IEnumerable{Guid}, string, T, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task UpdatePropertyAsync_Updates_Property_Of_Multiple_Entities_In_Database()
	{
		// Arrange
		FileModel[] entities = [.. TestUtils.CreateFiles(3)];

		string newName = AppUtils.CreateRandomString(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.GetAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<bool>())
				.Returns([.. entities]);

			IDbContextService dbConnection = Substitute.For<IDbContextService>();

			dbConnection
				.SaveChangesAsync()
				.Returns(entities.Length);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbConnection);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.UpdatePropertyAsync(
			entities.Select(x => x.Id),
			nameof(ExplorerModelBase.Name),
			newName);

		// Assert
		result
			.Should()
			.BeTrue();

		entities
			.Should()
			.OnlyContain(x => x.Name == newName);
	}
	#endregion

	#region Service
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
