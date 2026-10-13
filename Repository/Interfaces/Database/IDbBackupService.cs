using Microsoft.Data.Sqlite;
using Repository.Exceptions;
using Repository.Services.Database;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces.Database;

/// <summary>
/// Makes copies of the database and puts them back in its place.
/// </summary>
public interface IDbBackupService
{
	#region Methods
	/// <summary>
	/// Copies the database into a file through the SQLite backup API.
	/// </summary>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">One of the two files could not be opened, or the copy failed.</exception>
	Task CopyToAsync(string filePath, CancellationToken token = default);

	/// <summary>
	/// Makes a temporary copy of the database; the copy is erased when the returned instance is disposed.
	/// </summary>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The copy could not be made.</exception>
	Task<DatabaseBackup?> CreateBackupAsync(CancellationToken token = default);

	/// <summary>
	/// Restores database from backup.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The copy could not be written over the database.</exception>
	Task RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default);
	#endregion
}
