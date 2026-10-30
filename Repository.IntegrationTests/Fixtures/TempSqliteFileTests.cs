using AwesomeAssertions;
using System;
using System.Globalization;
using System.IO;

namespace Repository.IntegrationTests.Fixtures;

[TestFixture(Description = $@"Tests of ""{nameof(TempSqliteFile)}"" type")]
internal class TempSqliteFileTests
{
	#region Methods
	/// <summary>
	/// <see cref="TempSqliteFile.DeleteFoldersOfFinishedRuns" />: deletes a folder not named after a process,
	/// as the runs before the folders per process left them.
	/// </summary>
	[Test]
	public void DeleteFoldersOfFinishedRuns_Deletes_A_Folder_Not_Named_After_A_Process()
	{
		// Arrange
		using TempSqliteFile file = new();

		string rootDirectoryPath = Path.GetDirectoryName(file.FilePath)!;

		string directoryPath = Path.Combine(rootDirectoryPath, Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(directoryPath);

		// Act
		TempSqliteFile.DeleteFoldersOfFinishedRuns(rootDirectoryPath);

		// Assert
		Directory.Exists(directoryPath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="TempSqliteFile.DeleteFoldersOfFinishedRuns" />: deletes the folder of a process that is gone.
	/// </summary>
	[Test]
	public void DeleteFoldersOfFinishedRuns_Deletes_The_Folder_Of_A_Process_That_Is_Gone()
	{
		// Arrange
		using TempSqliteFile file = new();

		string rootDirectoryPath = Path.GetDirectoryName(file.FilePath)!;

		// No process gets an id that large.
		string directoryPath = Path.Combine(rootDirectoryPath, int.MaxValue.ToString(CultureInfo.InvariantCulture));

		Directory.CreateDirectory(directoryPath);

		// Act
		TempSqliteFile.DeleteFoldersOfFinishedRuns(rootDirectoryPath);

		// Assert
		Directory.Exists(directoryPath)
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="TempSqliteFile.DeleteFoldersOfFinishedRuns" />: keeps the folder of a running process,
	/// as its tests still use it.
	/// </summary>
	[Test]
	public void DeleteFoldersOfFinishedRuns_Keeps_The_Folder_Of_A_Running_Process()
	{
		// Arrange
		using TempSqliteFile file = new();

		string rootDirectoryPath = Path.GetDirectoryName(file.FilePath)!;

		string directoryPath = Path.Combine(rootDirectoryPath, Environment.ProcessId.ToString(CultureInfo.InvariantCulture));

		Directory.CreateDirectory(directoryPath);

		// Act
		TempSqliteFile.DeleteFoldersOfFinishedRuns(rootDirectoryPath);

		// Assert
		Directory.Exists(directoryPath)
			.Should()
			.BeTrue();
	}
	#endregion
}
