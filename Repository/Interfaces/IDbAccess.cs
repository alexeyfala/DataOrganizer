using Entities.Abstract;
using Entities.Interfaces;
using Entities.Models;
using Repository.DTO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
	/// Adds <see cref="FileModel.Hotkeys" /> to the entity.
	/// </summary>
	Task<HotkeyModel[]> AddHotkeysAsync(
		Guid fileId,
		CodeMaskPair[] hotkeys,
		CancellationToken token = default);

	/// <summary>
	/// Tries to backup database in file, and returns a path to it.
	/// </summary>
	bool BackupDatabase([NotNullWhen(true)] out string? backupFilePath);

	/// <summary>
	/// Establishes a connection to the database.
	/// </summary>
	Task ConnectAsync(
		bool useMigrations,
		CancellationToken token = default);

	/// <inheritdoc cref="IExplorerModelBaseRepository.CountOfAsync(Expression{Func{ExplorerModelBase, bool}}, CancellationToken)" />
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

	/// <inheritdoc cref="IFilesRepository.GetAllAsync(bool, bool, CancellationToken, string[])" />
	Task<FileModel[]> GetAllFilesAsync(
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default,
		params string[] excludedProperties);

	/// <inheritdoc cref="IFoldersRepository.GetAllAsync(bool, CancellationToken)" />
	Task<FolderModel[]> GetAllFoldersAsync(
		bool trackChanges = false,
		CancellationToken token = default);

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
	/// Restores database from backup.
	/// </summary>
	Task RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default);

	/// <summary>
	/// Updates the properties of an entity in the database.
	/// </summary>
	Task<bool> UpdatePropertiesAsync<T>(
		T dtoSource,
		CancellationToken token,
		params string[] propertyNames) where T : class, IIdentity;

	/// <inheritdoc cref="UpdatePropertiesAsync{T}(T, CancellationToken, string[])" />
	Task<bool> UpdatePropertiesAsync(
		Guid id,
		CancellationToken token,
		params PropertyNameValuePair[] properties);

	/// <summary>
	/// Updates the properties of entities in the database.
	/// </summary>
	Task<bool> UpdatePropertiesAsync(
		IDictionary<Guid, PropertyNameValuePair[]> relations,
		CancellationToken token = default);

	/// <summary>
	/// Updates the property of an entity in the database.
	/// </summary>
	Task<bool> UpdatePropertyAsync<T>(
		Guid id,
		string propertyName,
		T value,
		CancellationToken token = default);

	/// <summary>
	/// Updates property of entities in the database by identifiers.
	/// </summary>
	Task<bool> UpdatePropertyAsync<T>(
		IEnumerable<Guid> identifiers,
		string propertyName,
		T value,
		CancellationToken token = default);
	#endregion
}
