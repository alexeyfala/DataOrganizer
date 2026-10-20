using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Services.Exchange;
using NSubstitute;
using Repository.Dto;
using Repository.Interfaces.Database;
using System.Threading;
using System.Threading.Tasks;
using TestSupport.Common;
using TestSupport.Models;

namespace DataOrganizer.UnitTests.Services.Exchange;

[TestFixture(Description = $@"Tests of ""{nameof(SqliteExchangeFormat)}"" type")]
internal class SqliteExchangeFormatTests
{
	#region Data
	/// <summary>
	/// The path of the exchange file.
	/// </summary>
	private static readonly string FilePath = TestPath.Combine("exchange", "organizer.sqlite");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SqliteExchangeFormat.ExportAsync" />: the database is copied into the file.
	/// </summary>
	[Test]
	public async Task ExportAsync_Copies_The_Database()
	{
		// Arrange
		IDbBackupService dbBackupService = Substitute.For<IDbBackupService>();

		using AutoMock mock = AutoMock.GetLoose(builder => builder.RegisterInstance(dbBackupService));

		SqliteExchangeFormat sut = mock.Create<SqliteExchangeFormat>();

		// Act
		await sut.ExportAsync(FilePath);

		// Assert
		await dbBackupService
			.Received(1)
			.CopyToAsync(FilePath, Arg.Any<CancellationToken>());
	}

	/// <summary>
	/// <see cref="SqliteExchangeFormat.ReadAsync" />: a file that is not a database reads as <c>null</c>, without an
	/// attempt to read objects from it.
	/// </summary>
	[Test]
	public async Task ReadAsync_Gives_Null_For_A_File_That_Is_Not_A_Database()
	{
		// Arrange
		IDbImporter dbImporter = Substitute.For<IDbImporter>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			// The file is not a database, though that is also the default answer.
			dbImporter
				.IsValidSqliteDatabase(FilePath)
				.Returns(false);

			builder.RegisterInstance(dbImporter);
		});

		SqliteExchangeFormat sut = mock.Create<SqliteExchangeFormat>();

		// Act
		LoadedEntities? result = await sut.ReadAsync(FilePath);

		// Assert
		result
			.Should()
			.BeNull();

		dbImporter
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
			IDbImporter dbImporter = Substitute.For<IDbImporter>();

			dbImporter
				.IsValidSqliteDatabase(FilePath)
				.Returns(true);

			dbImporter
				.LoadEntities(FilePath)
				.Returns(loaded);

			builder.RegisterInstance(dbImporter);
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
