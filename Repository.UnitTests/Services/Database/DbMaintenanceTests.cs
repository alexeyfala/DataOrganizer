using Autofac;
using Autofac.Extras.Moq;
using NSubstitute;
using Repository.Interfaces.Database;
using Repository.Services.Database;
using Shared.Interfaces;
using System.IO;

namespace Repository.UnitTests.Services.Database;

[TestFixture(Description = $@"Tests of ""{nameof(DbMaintenance)}"" type")]
internal class DbMaintenanceTests
{
	#region Data
	/// <summary>
	/// Path of the database used in the tests of the leftover copies.
	/// </summary>
	private static readonly string DatabaseFilePath = Path.Combine(
		Path.GetTempPath(),
		"Database",
		"DataOrganizer.sqlite");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DbMaintenance.ErasePendingBackups" />: nothing is erased when there is nothing left behind.
	/// </summary>
	[Test]
	public void ErasePendingBackups_Erases_Nothing_Without_Leftovers()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(DatabaseFilePath);

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileSystem);
		});

		DbMaintenance sut = mock.Create<DbMaintenance>();

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

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(DatabaseFilePath);

			fileSystem
				.DirectoryExists(directoryPath)
				.Returns(true);

			fileSystem
				.EnumerateFiles(directoryPath)
				.Returns(leftovers);

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileSystem);
		});

		DbMaintenance sut = mock.Create<DbMaintenance>();

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

		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		using AutoMock mock = AutoMock.GetLoose(builder =>
		{
			IDbContextService dbContextService = Substitute.For<IDbContextService>();

			dbContextService
				.GetDbFilePath()
				.Returns(DatabaseFilePath);

			fileSystem
				.DirectoryExists(directoryPath)
				.Returns(true);

			fileSystem
				.EnumerateFiles(directoryPath)
				.Returns([lockedFilePath, nextFilePath]);

			fileSystem
				.When(x => x.EraseAndDeleteFile(lockedFilePath))
				.Throw(new IOException());

			builder.RegisterInstance(dbContextService);

			builder.RegisterInstance(fileSystem);
		});

		DbMaintenance sut = mock.Create<DbMaintenance>();

		// Act
		sut.ErasePendingBackups();

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteFile(nextFilePath);
	}
	#endregion
}
