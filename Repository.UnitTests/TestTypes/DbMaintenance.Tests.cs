using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Repository.Interfaces;
using Repository.Services;
using Repository.UnitTests.Helpers;
using Shared.Interfaces;
using System.IO;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DbMaintenance)}"" type")]
internal class DbMaintenanceTests
{
	#region Methods
	/// <summary>
	/// <see cref="DbMaintenance.EraseFreePagesOnceAsync" />: rewrites a database that still holds free pages and stamps it.
	/// </summary>
	[Test]
	public async Task EraseFreePagesOnceAsync_Rewrites_A_Database_With_Free_Pages()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using SqliteConnection connection = file.Open();

		FillAndClear(connection);

		TempSqliteFile
			.Read(connection, FreePagesQuery)
			.Should()
			.BeGreaterThan(0L);

		DbMaintenance sut = CreateSut(connection);

		// Act
		await sut.EraseFreePagesOnceAsync();

		// Assert
		TempSqliteFile
			.Read(connection, FreePagesQuery)
			.Should()
			.Be(0L);

		TempSqliteFile
			.Read(connection, VersionQuery)
			.Should()
			.Be(1L);
	}

	/// <summary>
	/// <see cref="DbMaintenance.EraseFreePagesOnceAsync" />: leaves a database that has already been rewritten alone.
	/// </summary>
	[Test]
	public async Task EraseFreePagesOnceAsync_Skips_An_Already_Stamped_Database()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using SqliteConnection connection = file.Open();

		TempSqliteFile.Execute(connection, "PRAGMA user_version = 1;");

		FillAndClear(connection);

		long freePages = TempSqliteFile.Read(connection, FreePagesQuery);

		DbMaintenance sut = CreateSut(connection);

		// Act
		await sut.EraseFreePagesOnceAsync();

		// Assert
		TempSqliteFile
			.Read(connection, FreePagesQuery)
			.Should()
			.Be(freePages);
	}

	/// <summary>
	/// <see cref="DbMaintenance.EraseFreePagesOnceAsync" />: stamps a database that has nothing to erase.
	/// </summary>
	[Test]
	public async Task EraseFreePagesOnceAsync_Stamps_A_Database_Without_Free_Pages()
	{
		// Arrange
		using TempSqliteFile file = new();

		await using SqliteConnection connection = file.Open();

		TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");

		DbMaintenance sut = CreateSut(connection);

		// Act
		await sut.EraseFreePagesOnceAsync();

		// Assert
		TempSqliteFile
			.Read(connection, VersionQuery)
			.Should()
			.Be(1L);
	}

	/// <summary>
	/// <see cref="DbMaintenance.ErasePendingBackups" />: nothing is erased when there is nothing left behind.
	/// </summary>
	[Test]
	public void ErasePendingBackups_Erases_Nothing_Without_Leftovers()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		DbMaintenance sut = CreateSut(fileSystem);

		// Act
		sut.ErasePendingBackups();

		// Assert
		fileSystem
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DbMaintenance.ErasePendingBackups" />: erases the copies left by an interrupted session.
	/// </summary>
	[Test]
	public void ErasePendingBackups_Erases_The_Leftover_Copies()
	{
		// Arrange
		string directoryPath = DatabaseBackup.GetDirectoryPath(DatabaseFilePath);

		string[] leftovers =
		[
			Path.Combine(directoryPath, "first.sqlite"),
			Path.Combine(directoryPath, "second.sqlite-journal")
		];

		IFileSystem fileSystem = CreateFileSystem(leftovers);

		DbMaintenance sut = CreateSut(fileSystem);

		// Act
		sut.ErasePendingBackups();

		// Assert
		foreach (string filePath in leftovers)
		{
			fileSystem
				.Received(1)
				.EraseAndDeleteFile(filePath);
		}
	}

	/// <summary>
	/// <see cref="DbMaintenance.ErasePendingBackups" />: a copy that cannot be erased does not stop the others.
	/// </summary>
	[Test]
	public void ErasePendingBackups_Survives_A_Failure_To_Erase()
	{
		// Arrange
		string directoryPath = DatabaseBackup.GetDirectoryPath(DatabaseFilePath);

		string lockedFilePath = Path.Combine(directoryPath, "locked.sqlite");

		string nextFilePath = Path.Combine(directoryPath, "next.sqlite");

		IFileSystem fileSystem = CreateFileSystem([lockedFilePath, nextFilePath]);

		fileSystem
			.When(x => x.EraseAndDeleteFile(lockedFilePath))
			.Throw(new IOException());

		DbMaintenance sut = CreateSut(fileSystem);

		// Act
		sut.ErasePendingBackups();

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteFile(nextFilePath);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Query reporting the number of pages the database keeps for reuse.
	/// </summary>
	private const string FreePagesQuery = "PRAGMA freelist_count;";

	/// <summary>
	/// Query reporting the stamp of the maintenance.
	/// </summary>
	private const string VersionQuery = "PRAGMA user_version;";

	/// <summary>
	/// Path of the database used in the tests of the leftover copies.
	/// </summary>
	private static readonly string DatabaseFilePath = Path.Combine(
		Path.GetTempPath(),
		"Database",
		"DataOrganizer.sqlite");

	/// <summary>
	/// Creates a file system holding the given copies of the database.
	/// </summary>
	private static IFileSystem CreateFileSystem(string[] leftovers)
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		string directoryPath = DatabaseBackup.GetDirectoryPath(DatabaseFilePath);

		fileSystem
			.IsDirectoryExists(directoryPath)
			.Returns(true);

		fileSystem
			.EnumerateFiles(directoryPath)
			.Returns(leftovers);

		return fileSystem;
	}

	/// <summary>
	/// Builds the service over the given file system.
	/// </summary>
	private static DbMaintenance CreateSut(IFileSystem fileSystem)
	{
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		dbContextService
			.GetDbFilePath()
			.Returns(DatabaseFilePath);

		using AutoMock mock = AutoMock.GetLoose();

		return mock.Create<DbMaintenance>(
			TypedParameter.From(dbContextService),
			TypedParameter.From(fileSystem));
	}

	/// <summary>
	/// Builds the service over the given connection.
	/// </summary>
	private static DbMaintenance CreateSut(SqliteConnection connection)
	{
		IDbContextService dbContextService = Substitute.For<IDbContextService>();

		dbContextService
			.GetDbConnection()
			.Returns(connection);

		using AutoMock mock = AutoMock.GetLoose();

		return mock.Create<DbMaintenance>(TypedParameter.From(dbContextService));
	}

	/// <summary>
	/// Fills the database and deletes everything, so that pages are left for reuse.
	/// </summary>
	private static void FillAndClear(SqliteConnection connection)
	{
		TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");

		TempSqliteFile.Execute(
			connection,
			"""
			WITH RECURSIVE Counter(Value) AS (SELECT 1 UNION ALL SELECT Value + 1 FROM Counter WHERE Value < 200)
			INSERT INTO Payloads (Payload) SELECT hex(randomblob(2048)) FROM Counter;
			""");

		TempSqliteFile.Execute(connection, "DELETE FROM Payloads;");
	}
	#endregion
}
