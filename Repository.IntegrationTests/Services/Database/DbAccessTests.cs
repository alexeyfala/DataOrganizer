using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Repository.Enums;
using Repository.IntegrationTests.Fixtures;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Interfaces;
using Shared.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.IntegrationTests.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbAccess)}"" type")]
internal class DbAccessTests
{
	#region Methods
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
	#endregion
}
