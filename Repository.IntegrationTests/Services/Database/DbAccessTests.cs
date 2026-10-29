using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Entities.Enums;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.DbContexts;
using Repository.Dto;
using Repository.Enums;
using Repository.IntegrationTests.Fixtures;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.IntegrationTests.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
	/// <summary>
	/// <see cref="DbAccess.ConnectAsync" />: a database with only the migrations this version knows connects and is
	/// brought up to date.
	/// </summary>
	[Test]
	public async Task ConnectAsync_Connects_To_A_Database_Of_This_Version()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");
		}

		const string known = "20260907183944_InitialCreate";

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.CanConnectAsync(Arg.Any<CancellationToken>())
				.Returns(true);

			dbContextService
				.GetDbFilePath()
				.Returns(file.FilePath);

			dbContextService
				.HasMigrations()
				.Returns(true);

			dbContextService
				.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>())
				.Returns([known]);

			dbContextService
				.GetKnownMigrations()
				.Returns([known]);

			builder.RegisterInstance(dbContextService);

			builder
				.RegisterType<FileSystem>()
				.As<IFileSystem>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.Connected);

		await dbContextService
			.Received(1)
			.MigrateAsync(Arg.Any<CancellationToken>());
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

		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbContextService
				.CanConnectAsync(Arg.Any<CancellationToken>())
				.Returns(true);

			dbContextService
				.GetDbFilePath()
				.Returns(file.FilePath);

			dbContextService
				.HasMigrations()
				.Returns(true);

			dbContextService
				.GetAppliedMigrationsAsync(Arg.Any<CancellationToken>())
				.Returns([known, "20991231235959_FromTheFuture"]);

			dbContextService
				.GetKnownMigrations()
				.Returns([known]);

			builder.RegisterInstance(dbContextService);

			builder
				.RegisterType<FileSystem>()
				.As<IFileSystem>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.SchemaTooNew);

		await dbContextService
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

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.CanConnectAsync(Arg.Any<CancellationToken>())
				.Returns(true);

			dbContextService
				.GetDbFilePath()
				.Returns(file.FilePath);

			dbContextService
				.HasMigrations()
				.Returns(true);

			dbContextService
				.MigrateAsync(Arg.Any<CancellationToken>())
				.ThrowsAsync(new InvalidOperationException(@"Table ""Payloads"" already exists"));

			builder.RegisterInstance(dbContextService);

			builder
				.RegisterType<FileSystem>()
				.As<IFileSystem>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DbConnectionStatus result = await sut.ConnectAsync();

		// Assert
		result
			.Should()
			.Be(DbConnectionStatus.SchemaTooOld);
	}

	/// <summary>
	/// <see cref="DbAccess.CopyToAsync" />: the database is copied into the file, and nothing keeps the copy open
	/// afterwards.
	/// </summary>
	[Test]
	public async Task CopyToAsync_Copies_The_Database_Into_A_File_Nothing_Holds_Open()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");

			TempSqliteFile.Execute(connection, "INSERT INTO Payloads (Payload) VALUES ('kept');");
		}

		string copyFilePath = Path.Combine(Path.GetDirectoryName(file.FilePath)!, "Copy.db");

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(file.FilePath);

			builder.RegisterInstance(dbContextService);
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		await sut.CopyToAsync(copyFilePath);

		// Assert
		string copyConnectionString = new SqliteConnectionStringBuilder
		{
			DataSource = copyFilePath,
			Pooling = false
		}.ToString();

		await using (SqliteConnection copy = new(copyConnectionString))
		{
			copy.Open();

			TempSqliteFile
				.Read(copy, "SELECT COUNT(*) FROM Payloads;")
				.Should()
				.Be(1L);
		}

		// A connection left in the pool would hold the copy open, and a file held open cannot be deleted.
		Action delete = () => File.Delete(copyFilePath);

		delete
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="DbAccess.CreateBackupAsync" />: the copy appears in the folder of the copies and is gone once released.
	/// </summary>
	[Test]
	public async Task CreateBackupAsync_Creates_A_Copy_That_Lives_Until_It_Is_Released()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");
		}

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(file.FilePath);

			builder.RegisterInstance(dbContextService);

			builder
				.RegisterType<FileSystem>()
				.As<IFileSystem>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		DatabaseBackup? backup = await sut.CreateBackupAsync();

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
	/// <see cref="DbAccess.IsValidSqliteDatabase" />: a file that starts like a database and holds nothing else of one is
	/// refused.
	/// </summary>
	[Test]
	public void IsValidSqliteDatabase_Refuses_A_File_That_Only_Starts_Like_A_Database()
	{
		// Arrange
		using TempSqliteFile file = new();

		File.WriteAllBytes(file.FilePath, [.. "SQLite format 3\0"u8, .. new byte[84].AsSpan()]);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<FileSystem>()
			.As<IFileSystem>());

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = sut.IsValidSqliteDatabase(file.FilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.IsValidSqliteDatabase" />: a missing file is refused and not made, as SQLite would make one.
	/// </summary>
	[Test]
	public void IsValidSqliteDatabase_Refuses_A_Missing_File()
	{
		// Arrange
		using TempSqliteFile file = new();

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<FileSystem>()
			.As<IFileSystem>());

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = sut.IsValidSqliteDatabase(file.FilePath);

		// Assert
		result
			.Should()
			.BeFalse();

		File.Exists(file.FilePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.IsValidSqliteDatabase" />: an empty file is refused, though SQLite opens one as an empty
	/// database.
	/// </summary>
	[Test]
	public void IsValidSqliteDatabase_Refuses_An_Empty_File()
	{
		// Arrange
		using TempSqliteFile file = new();

		File.WriteAllBytes(file.FilePath, []);

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<FileSystem>()
			.As<IFileSystem>());

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		bool result = sut.IsValidSqliteDatabase(file.FilePath);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.IsValidSqliteDatabase" />: a real database is accepted and a file that only
	/// carries the name of one is refused, which is what keeps an import from opening anything at all.
	/// </summary>
	[Test]
	public void IsValidSqliteDatabase_Tells_A_Database_From_Another_File()
	{
		// Arrange
		using TempSqliteFile file = new();

		using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");
		}

		string foreignFilePath = Path.Combine(Path.GetDirectoryName(file.FilePath)!, "Foreign.db");

		File.WriteAllText(foreignFilePath, "A name is not a database.");

		using AutoMock mock = AutoMock.GetLoose(builder => builder
			.RegisterType<FileSystem>()
			.As<IFileSystem>());

		DbAccess sut = mock.Create<DbAccess>();

		// Act, Assert
		sut
			.IsValidSqliteDatabase(file.FilePath)
			.Should()
			.BeTrue();

		sut
			.IsValidSqliteDatabase(foreignFilePath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="DbAccess.LoadEntities" />: reads the folders and the files of a database file other than the one in use.
	/// </summary>
	[Test]
	public async Task LoadEntities_Reads_The_Folders_And_Files_Of_A_Database_File()
	{
		// Arrange
		using TempSqliteFile file = new();

		Guid folderId = Guid.NewGuid();

		Guid fileId = Guid.NewGuid();

		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseSqlite(file.ConnectionString)
			.Options;

		await using (SqliteDbContext context = new(options))
		{
			await context.Database.EnsureCreatedAsync();

			context.Add(new FolderEntity
			{
				Id = folderId,
				Kind = EntityKind.Folder,
				Name = "Folder"
			});

			context.Add(new FileEntity
			{
				Id = fileId,
				Kind = EntityKind.File,
				Name = "File",
				ParentId = folderId
			});

			await context.SaveChangesAsync();
		}

		using AutoMock mock = AutoMock.GetLoose();

		DbAccess sut = mock.Create<DbAccess>();

		// Act
		LoadedEntities result = sut.LoadEntities(file.FilePath);

		// Assert
		result.Folders.Select(static x => x.Id)
			.Should()
			.Equal(folderId);

		result.Files.Select(static x => x.Id)
			.Should()
			.Equal(fileId);
	}

	/// <summary>
	/// <see cref="DbAccess.RestoreFromBackupAsync" />: the copy takes the place of the database it was
	/// made from, so what was deleted after it was taken is there again.
	/// </summary>
	[Test]
	public async Task RestoreFromBackupAsync_Brings_The_Copy_Back()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");

			TempSqliteFile.Execute(connection, "INSERT INTO Payloads (Payload) VALUES ('kept');");
		}

		await using SqliteConnection contextConnection = file.Open();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(file.FilePath);

			dbContextService
				.GetDbConnection()
				.Returns(contextConnection);

			builder.RegisterInstance(dbContextService);

			builder
				.RegisterType<FileSystem>()
				.As<IFileSystem>();
		});

		DbAccess sut = mock.Create<DbAccess>();

		using DatabaseBackup? backup = await sut.CreateBackupAsync();

		backup
			.Should()
			.NotBeNull();

		await using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "DELETE FROM Payloads;");
		}

		// Act
		await sut.RestoreFromBackupAsync(backup.FilePath);

		// Assert
		await using SqliteConnection restored = file.Open();

		TempSqliteFile
			.Read(restored, "SELECT COUNT(*) FROM Payloads;")
			.Should()
			.Be(1L);
	}
	#endregion
}
