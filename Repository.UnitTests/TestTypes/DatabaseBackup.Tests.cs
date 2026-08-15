using AwesomeAssertions;
using NSubstitute;
using Repository.Services;
using Serilog.Core;
using Shared.Common;
using Shared.Interfaces;
using System;
using System.IO;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(DatabaseBackup)}"" type")]
internal class DatabaseBackupTests
{
	#region Data
	/// <summary>
	/// Path of the copy used in the tests.
	/// </summary>
	private const string FilePath = @"C:\Database\Backup.sqlite";

	/// <summary>
	/// Path of the database the copies are built from.
	/// </summary>
	private static readonly string DatabaseFilePath = Path.Combine(
		Path.GetTempPath(),
		"Database",
		"DataOrganizer.sqlite");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="DatabaseBackup.CreateFilePath" />: the copies are kept in a folder of their own, under unique names.
	/// </summary>
	[Test]
	public void CreateFilePath_Builds_A_Unique_Path_In_The_Backups_Folder()
	{
		// Act
		string filePath = DatabaseBackup.CreateFilePath(DatabaseFilePath);

		// Assert
		Path
			.GetDirectoryName(filePath)
			.Should()
			.Be(DatabaseBackup.GetDirectoryPath(DatabaseFilePath));

		Path
			.GetExtension(filePath)
			.Should()
			.Be(AppUtils.SQLiteExtension);

		DatabaseBackup
			.CreateFilePath(DatabaseFilePath)
			.Should()
			.NotBe(filePath);
	}

	/// <summary>
	/// <see cref="DatabaseBackup.Dispose" />: erases the copy of the database.
	/// </summary>
	[Test]
	public void Dispose_Erases_The_Copy()
	{
		// Arrange
		IFileSystem fileSystem = CreateFileSystem();

		DatabaseBackup sut = new(FilePath, fileSystem, Logger.None);

		// Act
		sut.Dispose();

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteFile(FilePath);
	}

	/// <summary>
	/// <see cref="DatabaseBackup.Dispose" />: erases the copy no more than once.
	/// </summary>
	[Test]
	public void Dispose_Erases_The_Copy_Only_Once()
	{
		// Arrange
		IFileSystem fileSystem = CreateFileSystem();

		DatabaseBackup sut = new(FilePath, fileSystem, Logger.None);

		// Act
		sut.Dispose();

		sut.Dispose();

		// Assert
		fileSystem
			.Received(1)
			.EraseAndDeleteFile(FilePath);
	}

	/// <summary>
	/// <see cref="DatabaseBackup.Dispose" />: a copy that is already gone is left alone.
	/// </summary>
	[Test]
	public void Dispose_Skips_A_Missing_Copy()
	{
		// Arrange
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		DatabaseBackup sut = new(FilePath, fileSystem, Logger.None);

		// Act
		sut.Dispose();

		// Assert
		fileSystem
			.DidNotReceive()
			.EraseAndDeleteFile(Arg.Any<string>());
	}

	/// <summary>
	/// <see cref="DatabaseBackup.Dispose" />: a failure to erase does not leave the disposal throwing.
	/// </summary>
	[Test]
	public void Dispose_Survives_A_Failure_To_Erase()
	{
		// Arrange
		IFileSystem fileSystem = CreateFileSystem();

		fileSystem
			.When(x => x.EraseAndDeleteFile(FilePath))
			.Throw(new UnauthorizedAccessException());

		DatabaseBackup sut = new(FilePath, fileSystem, Logger.None);

		// Act
		Action act = sut.Dispose;

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="DatabaseBackup.GetDirectoryPath" />: the folder of the copies sits next to the database.
	/// </summary>
	[Test]
	public void GetDirectoryPath_Points_Next_To_The_Database()
	{
		// Act
		string directoryPath = DatabaseBackup.GetDirectoryPath(DatabaseFilePath);

		// Assert
		Path
			.GetDirectoryName(directoryPath)
			.Should()
			.Be(Path.GetDirectoryName(DatabaseFilePath));

		directoryPath
			.Should()
			.NotBe(Path.GetDirectoryName(DatabaseFilePath));
	}

	/// <summary>
	/// <see cref="DatabaseBackup.GetLegacyFilePath" />: the copy of the previous versions lies next to the database.
	/// </summary>
	[Test]
	public void GetLegacyFilePath_Points_Next_To_The_Database()
	{
		// Act
		string filePath = DatabaseBackup.GetLegacyFilePath(DatabaseFilePath);

		// Assert
		filePath
			.Should()
			.Be(Path.Combine(Path.GetDirectoryName(DatabaseFilePath)!, "Backup" + AppUtils.SQLiteExtension));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a file system in which the copy exists.
	/// </summary>
	private static IFileSystem CreateFileSystem()
	{
		IFileSystem fileSystem = Substitute.For<IFileSystem>();

		fileSystem
			.IsFileExists(FilePath)
			.Returns(true);

		return fileSystem;
	}
	#endregion
}
