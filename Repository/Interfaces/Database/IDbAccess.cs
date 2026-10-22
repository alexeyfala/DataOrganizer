using Entities.Enums;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repository.Dto;
using Repository.Enums;
using Repository.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces.Database;

/// <summary>
/// Reads and writes the folders, the files and their properties.
/// </summary>
public interface IDbAccess
{
	#region Methods
	/// <summary>
	/// Adds an entity to the database.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="DbUpdateException">The entity could not be written.</exception>
	Task<ExplorerItemBase> AddEntityAsync(
		AddEntityParameters parameters,
		CancellationToken token = default);

	/// <summary>
	/// Adds a file sequence to the database.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="DbUpdateException">The files could not be written.</exception>
	Task AddFilesAsync(IEnumerable<FileEntity> files, CancellationToken token = default);

	/// <summary>
	/// Adds a folder sequence to the database.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="DbUpdateException">The folders could not be written.</exception>
	Task AddFoldersAsync(IEnumerable<FolderEntity> folders, CancellationToken token = default);

	/// <inheritdoc cref="IExplorerItemRepository.CountOfAsync" />
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	Task<int> CountOfAsync(
		Expression<Func<ExplorerItemBase, bool>> condition,
		CancellationToken token = default);

	/// <summary>
	/// Deletes a folder with everything in it, or a file with its hotkeys; <c>false</c> when there is no such object.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The rows could not be deleted.</exception>
	Task<bool> DeleteAsync(EntityKind kind, Guid id, CancellationToken token = default);

	/// <summary>
	/// Deletes <see cref="FileEntity.Hotkeys" /> from the database by file identifier.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The rows could not be deleted.</exception>
	Task<bool> DeleteHotkeysAsync(Guid fileId, CancellationToken token = default);

	/// <summary>
	/// <c>True</c> when an object with the specified ID exists in the database.
	/// </summary>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	Task<bool> ExistsAsync(Guid id, CancellationToken token = default);

	/// <inheritdoc cref="IFileRepository.GetAllAsync" />
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	Task<FileEntity[]> GetAllFilesAsync(
		OptionalFileProperties optionalProperties,
		CancellationToken token = default);

	/// <inheritdoc cref="IFolderRepository.GetAllAsync" />
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	Task<FolderEntity[]> GetAllFoldersAsync(CancellationToken token = default);

	/// <summary>
	/// Returns <see cref="ValidatedContents" />.
	/// </summary>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	Task<ValidatedContents> GetFileContentsAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Returns a sequence of <see cref="ValidatedContents" /> by file identifiers.
	/// </summary>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	IAsyncEnumerable<ValidatedContents> GetFileContentsRangeAsync(
		IEnumerable<Guid> ids,
		CancellationToken token = default);

	/// <summary>
	/// Returns <see cref="FileEntity.EditorState" />.
	/// </summary>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The database could not be read.</exception>
	Task<string?> GetFileEditorStateAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Gives a folder or a file a new name and date of change; <c>false</c> when there is no such object.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The properties could not be written.</exception>
	Task<bool> RenameAsync(
		EntityKind kind,
		Guid id,
		string name,
		DateTime updatedAt,
		CancellationToken token = default);

	/// <summary>
	/// Replaces <see cref="FileEntity.Hotkeys" /> of a file in one transaction, so that a failure leaves the old ones; returns
	/// the new ones.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="DbUpdateException">The hotkeys could not be written.</exception>
	/// <exception cref="SqliteException">The old hotkeys could not be deleted.</exception>
	Task<HotkeyEntity[]> ReplaceHotkeysAsync(
		Guid fileId,
		KeyStroke[] hotkeys,
		CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="FolderEntity.EncryptedDek" />; <c>false</c> when there is no such folder.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The property could not be written.</exception>
	Task<bool> UpdateEncryptedDekAsync(Guid folderId, byte[] encryptedDek, CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="FileEntity.Contents" /> with the date of change; a <c>null</c> date keeps the one the file has.
	/// <c>False</c> when there is no such file.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The properties could not be written.</exception>
	Task<bool> UpdateFileContentsAsync(
		Guid fileId,
		byte[] contents,
		DateTime? updatedAt,
		CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="FileEntity.EditorState" />; <c>false</c> when there is no such file.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The property could not be written.</exception>
	Task<bool> UpdateFileEditorStateAsync(Guid fileId, string editorState, CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="FolderEntity.IsExpanded" />; <c>false</c> when there is no such folder.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The property could not be written.</exception>
	Task<bool> UpdateIsExpandedAsync(Guid folderId, bool isExpanded, CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="FileEntity.IsFavorite" />; <c>false</c> when there is no such file.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The property could not be written.</exception>
	Task<bool> UpdateIsFavoriteAsync(Guid fileId, bool isFavorite, CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="ExplorerItemBase.IsSelected" /> of a folder or a file; <c>false</c> when there is no such object.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The property could not be written.</exception>
	Task<bool> UpdateIsSelectedAsync(
		EntityKind kind,
		Guid id,
		bool isSelected,
		CancellationToken token = default);

	/// <summary>
	/// Writes <see cref="ExplorerItemBase.Note" /> of a folder or a file with the date of change; <c>false</c> when there is
	/// no such object.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The properties could not be written.</exception>
	Task<bool> UpdateNoteAsync(
		EntityKind kind,
		Guid id,
		byte[]? note,
		DateTime updatedAt,
		CancellationToken token = default);

	/// <summary>
	/// Writes the rewritten contents, notes and wrapped key of a folder whose protection changes, in one transaction.
	/// </summary>
	/// <exception cref="DatabaseNotWritableException">The database does not accept changes.</exception>
	/// <exception cref="OperationCanceledException">The operation was cancelled.</exception>
	/// <exception cref="SqliteException">The transaction could not be carried through.</exception>
	Task UpdateProtectionAsync(ProtectionUpdate update, CancellationToken token = default);
	#endregion
}
