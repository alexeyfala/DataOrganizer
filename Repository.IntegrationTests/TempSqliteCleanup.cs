using Microsoft.Data.Sqlite;
using Repository.IntegrationTests.Fixtures;
using System.IO;

namespace Repository.IntegrationTests;

/// <summary>
/// Removes the folder of the temporary databases after the run, together with what an interrupted run left in it.
/// </summary>
[SetUpFixture]
internal sealed class TempSqliteCleanup
{
	#region Methods
	/// <summary>
	/// Deletes <see cref="TempSqliteFile.RootDirectoryPath" /> once every test of the assembly has run.
	/// </summary>
	[OneTimeTearDown]
	public void DeleteRootDirectory()
	{
		// A connection left in a pool keeps its file open, and Windows does not delete an open file.
		SqliteConnection.ClearAllPools();

		if (!Directory.Exists(TempSqliteFile.RootDirectoryPath))
		{
			return;
		}

		Directory.Delete(TempSqliteFile.RootDirectoryPath, recursive: true);
	}
	#endregion
}
