using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using CommonTestHelpers.Helpers;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.DTO.Entities.Models;
using Entities.Abstract;
using Entities.Enums;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Repository.DbContexts;
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
		SqliteDbContext dbContext = GetSqliteDbContextMock();

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
			TypedParameter.From(dbContext),
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

		await dbContext
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		IFilesRepository repository = Substitute.For<IFilesRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.AddFilesAsync(files);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.AddRangeAsync(Arg.Any<IEnumerable<FileModel>>(), Arg.Any<CancellationToken>());

		await dbContext
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		IFoldersRepository repository = Substitute.For<IFoldersRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.AddFoldersAsync(folders);

		// Assert
		result
			.Should()
			.BeTrue();

		await repository
			.Received()
			.AddRangeAsync(Arg.Any<IEnumerable<FolderModel>>(), Arg.Any<CancellationToken>());

		await dbContext
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		IHotkeysRepository repository = Substitute.For<IHotkeysRepository>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
		});

		DbAccess sut = mock.Create<DbAccess>();

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
			.AddAsync(Arg.Any<HotkeyModel>(), Arg.Any<CancellationToken>());

		await dbContext
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.ClearDatabase" />.
	/// </summary>
	[Test]
	public void ClearDatabase_Recreates_Database([Values] bool useMigrations)
	{
		// Arrange
		IDbContextService dbConnection = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbConnection
				.HasMigrations(Arg.Any<Assembly>())
				.Returns(useMigrations);

			builder.RegisterInstance(dbConnection);

			builder.RegisterInstance(GetSqliteDbContextMock());
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = sut.ClearDatabase();

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

			builder.RegisterInstance(GetSqliteDbContextMock());
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
				.CountOfAsync(Arg.Any<Expression<Func<ExplorerModelBase, bool>>>(), Arg.Any<CancellationToken>())
				.Returns(expectedCount);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(GetSqliteDbContextMock());
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
		SqliteDbContext dbContext = GetSqliteDbContextMock();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContext
				.SaveChangesAsync()
				.Returns(1);

			IFoldersRepository repository = Substitute.For<IFoldersRepository>();

			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>())
				.Returns(TestUtils.CreateFolder());

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteFolderAsync(default);

		// Assert
		result
			.Should()
			.BeTrue();

		await dbContext
			.Received()
			.SaveChangesAsync();
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
		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(GetSqliteDbContextMock()));

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

			builder.RegisterInstance(GetSqliteDbContextMock());
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

			builder.RegisterInstance(GetSqliteDbContextMock());
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

			builder.RegisterInstance(GetSqliteDbContextMock());
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

			builder.RegisterInstance(GetSqliteDbContextMock());
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

			builder.RegisterInstance(GetSqliteDbContextMock());
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
				.IsExistsAsync(Arg.Any<Expression<Func<ExplorerModelBase, bool>>>(), Arg.Any<CancellationToken>())
				.Returns(true);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(GetSqliteDbContextMock());
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>(), Arg.Any<bool>())
				.Returns(entity);

			dbContext
				.SaveChangesAsync()
				.Returns(1);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.FirstOrDefaultAsync(firstEntity.Id, Arg.Any<bool>())
				.Returns(firstEntity);

			repository
				.FirstOrDefaultAsync(secondEntity.Id, Arg.Any<bool>())
				.Returns(secondEntity);

			dbContext
				.SaveChangesAsync()
				.Returns(2);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		FileModelDto dto = TestUtils.CreateFileDto();

		dto.IsSelected = true;

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			repository
				.FirstOrDefaultAsync(Arg.Any<Guid>(), Arg.Any<bool>())
				.Returns(entity);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
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

		await dbContext
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

			SqliteDbContext dbContext = GetSqliteDbContextMock();

			dbContext
				.SaveChangesAsync()
				.Returns(1);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
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

		SqliteDbContext dbContext = GetSqliteDbContextMock();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.GetAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
				.Returns([.. entities]);

			dbContext
				.SaveChangesAsync()
				.Returns(entities.Length);

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
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
	/// Creates mock for <see cref="SqliteDbContext" />.
	/// </summary>
	private static SqliteDbContext GetSqliteDbContextMock()
	{
		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return Substitute.For<SqliteDbContext>(options);
	}
	#endregion
}
