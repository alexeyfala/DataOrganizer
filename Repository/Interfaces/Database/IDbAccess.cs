using Entities.Models;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Dto;
using Repository.Enums;
using Repository.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces.Database;

/// <summary>
/// Provides methods for interacting with the database.
/// </summary>
public interface IDbAccess : IDisposable
{
	#region Properties
	/// <summary>
	/// The outcome of the last <see cref="ConnectAsync" />.
	/// </summary>
	DbConnectionStatus ConnectionStatus { get; }

	/// <summary>
	/// <c>True</c> while the database accepts changes.
	/// </summary>
	bool IsWritable { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Adds an entity to the database.
	/// </summary>
	Task<ExplorerItemBase?> AddEntityAsync(
		AddEntityParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Adds a file sequence to the database.
	/// </summary>
	Task<bool> AddFilesAsync(IEnumerable<FileEntity> files, CancellationToken token = default);

	/// <summary>
	/// Adds a folder sequence to the database.
	/// </summary>
	Task<bool> AddFoldersAsync(IEnumerable<FolderEntity> folders, CancellationToken token = default);

	/// <summary>
	/// Adds <see cref="FileEntity.Hotkeys" /> to the entity.
	/// </summary>
	Task<HotkeyEntity[]> AddHotkeysAsync(
		Guid fileId,
		KeyStroke[] hotkeys,
		CancellationToken token = default);

	/// <summary>
	/// Tries to backup database in file; the copy is erased when the returned instance is disposed.
	/// </summary>
	Task<DatabaseBackup?> BackupDatabaseAsync(CancellationToken token = default);

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
	/// Establishes a connection to the database, creating or migrating it as needed.
	/// Tells apart a file that cannot be opened and a schema that does not match this version;
	/// housekeeping failures are logged and do not affect the result.
	/// </summary>
	Task<DbConnectionStatus> ConnectAsync(CancellationToken token = default);

	/// <inheritdoc cref="IExplorerItemRepository.CountOfAsync" />
	Task<int> CountOfAsync(
		Expression<Func<ExplorerItemBase, bool>> condition,
		CancellationToken token = default);

	/// <summary>
	/// Deletes an <see cref="FileEntity" /> from the database by identifier.
	/// </summary>
	Task<bool> DeleteFileAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Deletes an <see cref="FolderEntity" /> from the database by identifier.
	/// </summary>
	Task<bool> DeleteFolderAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Deletes <see cref="FileEntity.Hotkeys" /> from the database by file identifier.
	/// </summary>
	Task<bool> DeleteHotkeysAsync(Guid fileId, CancellationToken token = default);

	/// <summary>
	/// <c>True</c> when an object with the specified ID exists in the database.
	/// </summary>
	Task<bool> ExistsAsync(Guid id, CancellationToken token = default);

	/// <inheritdoc cref="IFileRepository.GetAllAsync" />
	Task<FileEntity[]> GetAllFilesAsync(
		OptionalFileProperties optionalProperties,
		CancellationToken token = default);

	/// <inheritdoc cref="IFolderRepository.GetAllAsync" />
	Task<FolderEntity[]> GetAllFoldersAsync(CancellationToken token = default);

	/// <inheritdoc cref="IDbContextService.GetDbFilePath" />
	string GetDbFilePath();

	/// <summary>
	/// Returns <see cref="ValidatedContents" />.
	/// </summary>
	Task<ValidatedContents> GetFileContentsAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Returns a sequence of <see cref="ValidatedContents" /> by file identifiers.
	/// </summary>
	IAsyncEnumerable<ValidatedContents> GetFileContentsRangeAsync(
		IEnumerable<Guid> ids,
		CancellationToken token = default);

	/// <summary>
	/// Returns <see cref="FileEntity.EditorState" />.
	/// </summary>
	Task<string?> GetFileEditorStateAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// <c>True</c> when a SQLite database is valid.
	/// </summary>
	public bool IsValidSqliteDatabase(string dataSource, bool deepCheck = false);

	/// <summary>
	/// Loads all entities from the specified database.
	/// </summary>
	LoadedEntities LoadEntities(string dataSource);

	/// <summary>
	/// Restores database from backup.
	/// </summary>
	Task<bool> RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default);

	/// <summary>
	/// Updates properties of multiple <see cref="FileEntity" /> and <see cref="FolderEntity" /> entities
	/// in a single transaction. An empty set of updates is not a failure.
	/// </summary>
	Task<bool> UpdateFileAndFolderPropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> fileUpdates,
		IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> folderUpdates,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of <see cref="FileEntity" />.
	/// </summary>
	Task<bool> UpdateFilePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FileEntity>>[] setters,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of multiple <see cref="FileEntity" /> entities in a single transaction.
	/// </summary>
	Task<bool> UpdateFilePropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> updates,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of <see cref="FolderEntity" />.
	/// </summary>
	Task<bool> UpdateFolderPropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FolderEntity>>[] setters,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of multiple <see cref="FolderEntity" /> entities in a single transaction.
	/// </summary>
	Task<bool> UpdateFolderPropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> updates,
		CancellationToken token = default);
	#endregion
}
