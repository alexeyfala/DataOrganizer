using Entities.Abstract;
using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.DTO;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Provides methods for interacting with the database.
/// </summary>
public interface IDbAccess : IDisposable
{
	#region Methods
	/// <summary>
	/// Adds an entity to the database.
	/// </summary>
	Task<ExplorerModelBase?> AddEntityAsync(
		AddEntityParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Adds a file sequence to the database.
	/// </summary>
	Task<bool> AddFilesAsync(IEnumerable<FileModel> files, CancellationToken token = default);

	/// <summary>
	/// Adds a folder sequence to the database.
	/// </summary>
	Task<bool> AddFoldersAsync(IEnumerable<FolderModel> folders, CancellationToken token = default);

	/// <summary>
	/// Adds <see cref="FileModel.Hotkeys" /> to the entity.
	/// </summary>
	Task<HotkeyModel[]> AddHotkeysAsync(
		Guid fileId,
		CodeMaskPair[] hotkeys,
		CancellationToken token = default);

	/// <summary>
	/// Tries to backup database in file, and returns a path to it.
	/// </summary>
	Task<string?> BackupDatabaseAsync(CancellationToken token = default);

	/// <summary>
	/// Backups SQLite database.
	/// </summary>
	Task BackupSqliteDatabaseAsync(
		BackupSqliteParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Completely clears the database.
	/// </summary>
	Task<bool> ClearDatabaseAsync(CancellationToken token = default);

	/// <summary>
	/// Establishes a connection to the database.
	/// </summary>
	Task ConnectAsync(CancellationToken token = default);

	/// <inheritdoc cref="IExplorerModelBaseRepository.CountOfAsync" />
	Task<int> CountOfAsync(
		Expression<Func<ExplorerModelBase, bool>> condition,
		CancellationToken token = default);

	/// <summary>
	/// Deletes an <see cref="FileModel" /> from the database by identifier.
	/// </summary>
	Task<bool> DeleteFileAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Deletes an <see cref="FolderModel" /> from the database by identifier.
	/// </summary>
	Task<bool> DeleteFolderAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Deletes <see cref="FileModel.Hotkeys" /> from the database by file identifier.
	/// </summary>
	Task<bool> DeleteHotkeysAsync(Guid fileId, CancellationToken token = default);

	/// <inheritdoc cref="IFilesRepository.GetAllAsync" />
	Task<FileModel[]> GetAllFilesAsync(CancellationToken token = default);

	/// <inheritdoc cref="IFoldersRepository.GetAllAsync" />
	Task<FolderModel[]> GetAllFoldersAsync(CancellationToken token = default);

	/// <inheritdoc cref="IDbContextService.GetDbFilePath" />
	string GetDbFilePath();

	/// <summary>
	/// Returns <see cref="ContentsIsValidPair" />.
	/// </summary>
	Task<ContentsIsValidPair> GetFileContentsAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Returns <see cref="FileModel.Properties" />.
	/// </summary>
	Task<string?> GetFilePropertiesAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Returns a sequense of <see cref="ContentsIsValidPair" /> by file identifiers.
	/// </summary>
	IAsyncEnumerable<ContentsIsValidPair> GetFilesContentsAsync(
		IEnumerable<Guid> identifiers,
		CancellationToken token = default);

	/// <summary>
	/// Returns <c>True</c> if an object with the specified ID exists in the database.
	/// </summary>
	Task<bool> IsExistsAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Returns <c>True</c> if a SQLite database is valid.
	/// </summary>
	public bool IsValidSQLiteDatabase(string dataSource, bool deepCheck = false);

	/// <summary>
	/// Loads all entities from the specified database.
	/// </summary>
	LoadFromDbResult LoadFromDb(string dataSource);

	/// <summary>
	/// Restores database from backup.
	/// </summary>
	Task<bool> RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default);

	/// <summary>
	/// Updates properties of <see cref="FileModel" />.
	/// </summary>
	Task<bool> UpdateFilePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FileModel>>[] setters,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of multiple <see cref="FileModel" /> entities in a single transaction.
	/// </summary>
	Task<bool> UpdateFilePropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of <see cref="FolderModel" />.
	/// </summary>
	Task<bool> UpdateFolderPropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FolderModel>>[] setters,
		CancellationToken token = default);
	#endregion
}
