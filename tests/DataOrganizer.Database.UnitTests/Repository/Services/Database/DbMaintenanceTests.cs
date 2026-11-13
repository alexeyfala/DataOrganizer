using Autofac;
using Autofac.Extras.Moq;
using AwesomeAssertions;
using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Database.Repository.Services.Database;
using DataOrganizer.Shared.Interfaces;
using DataOrganizer.TestSupport.Common;
using NSubstitute;
using System.IO;
using System.IO.Abstractions;
using Testably.Abstractions.Testing;

namespace DataOrganizer.Database.UnitTests.Repository.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbMaintenance)}"" type")]
internal class DbMaintenanceTests
{
	#region Data
	/// <summary>
	/// Path of the database used in the tests of the leftover copies.
	/// </summary>
	private static readonly string DatabaseFilePath = TestPath.Combine("Database", "DataOrganizer.sqlite");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DbMaintenance.ErasePendingBackups" />: nothing is erased when there is nothing left behind.
	/// </summary>
	[Test]
	public void ErasePendingBackups_Erases_Nothing_Without_Leftovers()
	{
		// Arrange
		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		MockFileSystem files = new();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(DatabaseFilePath);

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance<IFileSystem>(files);
		});

		DbMaintenance sut = mock.Create<DbMaintenance>();

		// Act
		sut.ErasePendingBackups();

		// Assert
		files.Statistics.Directory.Methods
			.Should()
			.NotContain(x => x.Name == nameof(IDirectory.EnumerateFiles));

		fileEraser
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

		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			MockFileSystem files = new();

			dbContextService
				.GetDbFilePath()
				.Returns(DatabaseFilePath);

			files
				.Directory
				.CreateDirectory(directoryPath);

			foreach (string filePath in leftovers)
			{
				files
					.File
					.WriteAllBytes(filePath, [1]);
			}

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance<IFileSystem>(files);
		});

		DbMaintenance sut = mock.Create<DbMaintenance>();

		// Act
		sut.ErasePendingBackups();

		// Assert
		foreach (string filePath in leftovers)
		{
			fileEraser
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

		IFileEraser fileEraser = Substitute.For<IFileEraser>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			MockFileSystem files = new();

			dbContextService
				.GetDbFilePath()
				.Returns(DatabaseFilePath);

			files
				.Directory
				.CreateDirectory(directoryPath);

			files
				.File
				.WriteAllBytes(lockedFilePath, [1]);

			files
				.File
				.WriteAllBytes(nextFilePath, [1]);

			fileEraser
				.When(x => x.EraseAndDeleteFile(lockedFilePath))
				.Throw(new IOException());

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileEraser);

			builder.RegisterInstance<IFileSystem>(files);
		});

		DbMaintenance sut = mock.Create<DbMaintenance>();

		// Act
		sut.ErasePendingBackups();

		// Assert
		fileEraser
			.Received(1)
			.EraseAndDeleteFile(nextFilePath);
	}
	#endregion
}
