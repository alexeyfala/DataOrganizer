using Microsoft.Data.Sqlite;
using Repository.IntegrationTests.Fixtures;
using System.IO;

namespace Repository.IntegrationTests;

/// <summary>
/// Removes the folder of the temporary databases of this run after it, together with what finished or interrupted runs left.
/// </summary>
[SetUpFixture]
internal sealed class TempSqliteCleanup
{
	#region Methods
	/// <summary>
	/// Deletes <see cref="TempSqliteFile.RunDirectoryPath" /> once every test of the assembly has run,
	/// and the folders of the runs whose process is gone.
	/// </summary>
	[OneTimeTearDown]
	public void DeleteRunDirectories()
	{
		// A connection left in a pool keeps its file open, and Windows does not delete an open file.
		SqliteConnection.ClearAllPools();

		if (!Directory.Exists(TempSqliteFile.RootDirectoryPath))
		{
			return;
		}

		if (Directory.Exists(TempSqliteFile.RunDirectoryPath))
		{
			Directory.Delete(TempSqliteFile.RunDirectoryPath, recursive: true);
		}

		TempSqliteFile.DeleteFoldersOfFinishedRuns(TempSqliteFile.RootDirectoryPath);
	}
	#endregion
}
