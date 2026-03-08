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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
	/// <summary>
	/// Test of <see cref="DbAccess.AddEntityAsync(AddEntityParameters, CancellationToken)" />.
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
	/// Test of <see cref="DbAccess.DeleteFileAsync(Guid, CancellationToken)" />.
	/// </summary>
	[Test]
	public async Task DeleteFileAsync_Deletes_File_From_Database()
	{
		// Arrange
		SqliteDbContext dbContext = GetSqliteDbContextMock();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContext
				.SaveChangesAsync()
				.Returns(1);

			IFilesRepository repository = Substitute.For<IFilesRepository>();

			repository
				.GetAsync(Arg.Any<Guid>())
				.Returns(TestUtils.CreateFile());

			builder.RegisterInstance(repository);

			builder.RegisterInstance(dbContext);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = await sut.DeleteFileAsync(default);

		// Assert
		result
			.Should()
			.BeTrue();

		await dbContext
			.Received()
			.SaveChangesAsync();
	}

	/// <summary>
	/// Test of <see cref="DbAccess.DeleteFolderAsync(Guid, CancellationToken)" />.
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
				.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>())
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
	/// Test of <see cref="DbAccess.GetAllFilesAsync(bool, bool, string?, CancellationToken)" />.
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
	/// Test of <see cref="DbAccess.GetAllFoldersAsync(bool, CancellationToken)" />.
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
	/// Test of <see cref="DbAccess.UpdatePropertiesAsync{T}(Guid, T, CancellationToken, string[])" />.
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
				.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>())
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
		ExplorerModelBase entity = Substitute.For<ExplorerModelBase>();

		string newName = AppUtils.CreateRandomString(10);

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IExplorerModelBaseRepository repository = Substitute.For<IExplorerModelBaseRepository>();

			repository
				.GetAsync(Arg.Any<Guid>(), Arg.Any<bool>())
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
