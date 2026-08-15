using Microsoft.Data.Sqlite;
using Repository.Interceptors;
using System;
using System.Data.Common;
using System.IO;

namespace Repository.UnitTests.Helpers;

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
		_directoryPath = Path.Combine(
			Path.GetTempPath(),
			"DataOrganizerTests",
			Guid.NewGuid().ToString("N"));

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
	#endregion

	#region Methods
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
		try
		{
			Directory.Delete(_directoryPath, recursive: true);
		}
		catch (IOException)
		{
			// The folder is temporary; a locked file is not worth failing a test over.
		}
	}

	/// <summary>
	/// Opens a connection with the pragmas of the application applied.
	/// </summary>
	public SqliteConnection Open()
	{
		SqliteConnection connection = new(ConnectionString);

		SqlitePragmas.Open(connection);

		return connection;
	}
	#endregion
}
