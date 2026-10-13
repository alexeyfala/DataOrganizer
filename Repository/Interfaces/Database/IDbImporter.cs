using Microsoft.Data.Sqlite;
using Repository.Dto;
using Repository.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces.Database;

/// <summary>
/// Reads database files brought in from outside and clears the database for an import that replaces it.
/// </summary>
public interface IDbImporter
{
	#region Methods
	/// <summary>
	/// Completely clears the database.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be dropped and laid out anew.</exception>
	Task ClearDatabaseAsync(CancellationToken token = default);

	/// <summary>
	/// <c>True</c> when a SQLite database is valid.
	/// </summary>
	bool IsValidSqliteDatabase(string databaseFilePath);

	/// <summary>
	/// Loads all entities from the specified database.
	/// </summary>
	/// <exception cref="SqliteException">The file is not a database this version can read.</exception>
	LoadedEntities LoadEntities(string databaseFilePath);
	#endregion
}
