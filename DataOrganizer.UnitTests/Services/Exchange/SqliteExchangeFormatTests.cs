using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Exchange;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Exchange;

[TestFixture(Description = $@"Tests of ""{nameof(SqliteExchangeFormat)}"" type")]
internal class SqliteExchangeFormatTests
{
	#region Data
	/// <summary>
	/// The path of the exchange file.
	/// </summary>
	private const string FilePath = @"C:\exchange\organizer.sqlite";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SqliteExchangeFormat.ExportAsync" />: the database is copied into the file.
	/// </summary>
	[Test]
	public async Task ExportAsync_Copies_The_Database()
	{
		// Arrange
		const string databaseFilePath = @"C:\data\organizer.sqlite";

		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			dbAccess
				.GetDbFilePath()
				.Returns(databaseFilePath);

			builder.RegisterInstance(dbAccess);
		});

		SqliteExchangeFormat sut = mock.Create<SqliteExchangeFormat>();

		// Act
		await sut.ExportAsync(FilePath);

		// Assert
		await dbAccess.Received(1).CopyDatabaseAsync(
			new CopyDatabaseParameters
			{
				ClearDestinationPool = true,
				ClearSourcePool = false,
				DestinationFilePath = FilePath,
				SourceFilePath = databaseFilePath
			},
			Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="SqliteExchangeFormat.ReadAsync" />: a file that is not a database reads as <c>null</c>, without an
	/// attempt to read objects from it.
	/// </summary>
	[Test]
	public async Task ReadAsync_Gives_Null_For_A_File_That_Is_Not_A_Database()
	{
		// Arrange
		IDbAccess dbAccess = Substitute.For<IDbAccess>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The file is not a database, though that is also the default answer.
			dbAccess
				.IsValidSqliteDatabase(FilePath)
				.Returns(false);

			builder.RegisterInstance(dbAccess);
		});

		SqliteExchangeFormat sut = mock.Create<SqliteExchangeFormat>();

		// Act
		LoadedEntities? result = await sut.ReadAsync(FilePath);

		// Assert
		result
			.Should()
			.BeNull();

		dbAccess
			.DidNotReceive()
			.LoadEntities(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="SqliteExchangeFormat.ReadAsync" />: the folders and files of a database file are read out of it.
	/// </summary>
	[Test]
	public async Task ReadAsync_Reads_The_Objects_Of_The_Database_File()
	{
		// Arrange
		LoadedEntities loaded = new()
		{
			Files = [.. EntityFactory.CreateFiles(2)],
			Folders = [.. EntityFactory.CreateFolders(2)]
		};

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbAccess dbAccess = Substitute.For<IDbAccess>();

			dbAccess
				.IsValidSqliteDatabase(FilePath)
				.Returns(true);

			dbAccess
				.LoadEntities(FilePath)
				.Returns(loaded);

			builder.RegisterInstance(dbAccess);
		});

		SqliteExchangeFormat sut = mock.Create<SqliteExchangeFormat>();

		// Act
		LoadedEntities? result = await sut.ReadAsync(FilePath);

		// Assert
		result
			.Should()
			.BeSameAs(loaded);
	}
	#endregion
}
