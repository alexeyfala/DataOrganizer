using Microsoft.Data.Sqlite;
using Repository.Interceptors;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Repository.IntegrationTests.Fixtures;

/// <summary>
/// A SQLite database file in a private temporary folder, removed when the test ends.
/// </summary>
internal sealed class TempSqliteFile : IDisposable
{
	#region Data
	/// <summary>
	/// Folder holding the database file.
	/// </summary>
	private readonly string _directoryPath;
	#endregion

	#region Constructors
	public TempSqliteFile()
	{
		_directoryPath = Path.Combine(RunDirectoryPath, Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(_directoryPath);

		FilePath = Path.Combine(_directoryPath, "Test.db");

		// Pooling would keep the file handle alive past the test and block the cleanup.
		ConnectionString = new SqliteConnectionStringBuilder
		{
			DataSource = FilePath,
			Pooling = false
		}.ToString();
	}
	#endregion

	#region Properties
	/// <summary>
	/// Connection string of the database.
	/// </summary>
	public string ConnectionString { get; }

	/// <summary>
	/// Path of the database file.
	/// </summary>
	public string FilePath { get; }

	/// <summary>
	/// Folder that holds the folders of the test processes, one per process.
	/// </summary>
	public static string RootDirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "DataOrganizerTests");

	/// <summary>
	/// Folder of the temporary databases of this test process, named after its id.
	/// </summary>
	public static string RunDirectoryPath { get; } = Path.Combine(
		RootDirectoryPath,
		Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
	#endregion

	#region Methods
	/// <summary>
	/// Deletes the folders under <paramref name="rootDirectoryPath" /> that no running test process owns.
	/// </summary>
	public static void DeleteFoldersOfFinishedRuns(string rootDirectoryPath)
	{
		foreach (string directoryPath in Directory.GetDirectories(rootDirectoryPath))
		{
			if (int.TryParse(Path.GetFileName(directoryPath), NumberStyles.None, CultureInfo.InvariantCulture, out int processId)
				&& IsRunning(processId))
			{
				continue;
			}

			try
			{
				Directory.Delete(directoryPath, recursive: true);
			}
			catch (DirectoryNotFoundException)
			{
				// The cleanup of another process that ended at the same time got there first.
			}
		}
	}

	/// <summary>
	/// Executes a statement that returns nothing.
	/// </summary>
	public static void Execute(DbConnection connection, string sql)
	{
		using DbCommand command = connection.CreateCommand();

		command.CommandText = sql;

		command.ExecuteNonQuery();
	}

	/// <summary>
	/// Reads the single number a query reports.
	/// </summary>
	public static long Read(DbConnection connection, string sql)
	{
		using DbCommand command = connection.CreateCommand();

		command.CommandText = sql;

		return command.ExecuteScalar() is long number ? number : 0L;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		// A connection left in a pool keeps its file open, and Windows does not delete an open file.
		SqliteConnection.ClearAllPools();

		Directory.Delete(_directoryPath, recursive: true);
	}

	/// <summary>
	/// Opens a connection with the pragmas of the application applied.
	/// </summary>
	public SqliteConnection Open()
	{
		SqliteConnection connection = new(ConnectionString);

		SqlitePragmas.OpenConnection(connection);

		return connection;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> while a process with <paramref name="processId" /> runs.
	/// </summary>
	private static bool IsRunning(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);

			return true;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}
	#endregion
}
