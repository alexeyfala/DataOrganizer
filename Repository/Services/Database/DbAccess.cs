using Entities.Enums;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.DbContexts;
using Repository.Dto;
using Repository.Enums;
using Repository.Exceptions;
using Repository.Interceptors;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Threading.Extensions;
using Threading.Helpers;

namespace Repository.Services.Database;

/// <summary>
/// Works with the SQLite database in every role: connection, objects, backups and import.
/// </summary>
public sealed class DbAccess :
	IDbAccess,
	IDbBackupService,
	IDbConnector,
	IDbImporter,
	IDisposable
{
	#region Properties
	/// <inheritdoc />
	public DbConnectionStatus ConnectionStatus { get; private set; } = DbConnectionStatus.Connected;

	/// <inheritdoc />
	public bool IsWritable => ConnectionStatus is DbConnectionStatus.Connected;
	#endregion

	#region Data
	/// <inheritdoc cref="IDbContextService" />
	private readonly IDbContextService _dbContextService;

	/// <inheritdoc cref="IDbMaintenance" />
	private readonly IDbMaintenance _dbMaintenance;

	/// <inheritdoc cref="IExplorerItemRepository" />
	private readonly IExplorerItemRepository _explorerItemRepository;

	/// <inheritdoc cref="IFileRepository" />
	private readonly IFileRepository _fileRepository;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IFolderRepository" />
	private readonly IFolderRepository _folderRepository;

	/// <inheritdoc cref="IHotkeysRepository" />
	private readonly IHotkeysRepository _hotkeysRepository;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public DbAccess(
		IDbContextService dbContextService,
		IDbMaintenance dbMaintenance,
		IExplorerItemRepository explorerItemRepository,
		IFileRepository fileRepository,
		IFileSystem fileSystem,
		IFolderRepository folderRepository,
		IHotkeysRepository hotkeysRepository,
		ILogger logger,
		TimeProvider timeProvider)
	{
		_dbContextService = dbContextService;

		_dbMaintenance = dbMaintenance;

		_explorerItemRepository = explorerItemRepository;

		_fileRepository = fileRepository;

		_fileSystem = fileSystem;

		_folderRepository = folderRepository;

		_hotkeysRepository = hotkeysRepository;

		_logger = logger;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ExplorerItemBase> AddEntityAsync(
		AddEntityParameters parameters,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			ExplorerItemBase entity = IsFolder(parameters.Kind)
				? await AddFolderAsync(parameters, token).ConfigureAwait(false)
				: await AddFileAsync(parameters, token).ConfigureAwait(false);

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);

			return entity;
		}
		finally
		{
			_dbContextService.ClearTracking();
		}
	}

	/// <inheritdoc />
	public async Task AddFilesAsync(IEnumerable<FileEntity> files, CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			await _fileRepository
				.AddRangeAsync(files, token)
				.ConfigureAwait(false);

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);
		}
		finally
		{
			_dbContextService.ClearTracking();
		}
	}

	/// <inheritdoc />
	public async Task AddFoldersAsync(IEnumerable<FolderEntity> folders, CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			await _folderRepository
				.AddRangeAsync(folders, token)
				.ConfigureAwait(false);

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);
		}
		finally
		{
			_dbContextService.ClearTracking();
		}
	}

	/// <inheritdoc />
	public async Task ClearDatabaseAsync(CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		_dbContextService.EnsureDeleted();

		if (_dbContextService.HasMigrations())
		{
			_dbContextService.Migrate();
		}
		else
		{
			_dbContextService.EnsureCreated();
		}
	}

	/// <inheritdoc />
	public async Task<DbConnectionStatus> ConnectAsync(CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			_logger.LogInformation("Connecting to the database.");

			TryErasePendingBackups();

			bool hasDatabaseFile = _fileSystem.FileExists(_dbContextService.GetDbFilePath());

			DbConnectionStatus status = hasDatabaseFile
				? await GetSchemaStatusAsync(token).ConfigureAwait(false)
				: DbConnectionStatus.Connected;

			if (status is DbConnectionStatus.Connected)
			{
				DbConnectionStatus failureStatus = hasDatabaseFile
					? DbConnectionStatus.SchemaTooOld
					: DbConnectionStatus.FileUnreadable;

				// A database that has just been read holds data, so a failure here is about its schema.
				status = await TryUpdateSchemaAsync(failureStatus, token).ConfigureAwait(false);
			}

			ConnectionStatus = status;

			if (status is not DbConnectionStatus.Connected)
			{
				_logger.LogError($"The database cannot be worked with: {status}.", breakInDebugger: false);

				return status;
			}

			await TryEraseFreePagesOnceAsync(token).ConfigureAwait(false);

			return status;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			ConnectionStatus = DbConnectionStatus.FileUnreadable;

			return ConnectionStatus;
		}
	}

	/// <inheritdoc />
	public async Task CopyToAsync(string filePath, CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		CopyDatabaseParameters parameters = new()
		{
			ClearDestinationPool = true,
			ClearSourcePool = false,
			DestinationFilePath = filePath,
			SourceFilePath = GetDbFilePath()
		};

		CopyDatabase(parameters);
	}

	/// <inheritdoc />
	public async Task<int> CountOfAsync(
		Expression<Func<ExplorerItemBase, bool>> condition,
		CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		return await _explorerItemRepository
			.CountOfAsync(condition, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<DatabaseBackup?> CreateBackupAsync(CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		string dbFilePath = GetDbFilePath();

		if (!_fileSystem.FileExists(dbFilePath) || Path.GetDirectoryName(dbFilePath) is not { })
		{
			return null;
		}

		string backupFilePath = DatabaseBackup.CreateFilePath(dbFilePath);

		_fileSystem.CreateDirectory(DatabaseBackup.GetDirectoryPath(dbFilePath));

		CopyDatabaseParameters parameters = new()
		{
			ClearDestinationPool = true,
			ClearSourcePool = false,
			DestinationFilePath = backupFilePath,
			SourceFilePath = dbFilePath
		};

		CopyDatabase(parameters);

		if (!_fileSystem.FileExists(backupFilePath))
		{
			return null;
		}

		return new DatabaseBackup(
			backupFilePath,
			_fileSystem,
			_logger);
	}

	/// <inheritdoc />
	public async Task<bool> DeleteAsync(EntityKind kind, Guid id, CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		int count = IsFolder(kind)
			? await DeleteFolderTreeAsync(id, token).ConfigureAwait(false)
			: await DeleteFileWithHotkeysAsync(id, token).ConfigureAwait(false);

		return count > 0;
	}

	/// <inheritdoc />
	public async Task<bool> DeleteHotkeysAsync(Guid fileId, CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		int count = await _hotkeysRepository
			.RemoveRangeByOwnerIdAsync(fileId, token)
			.ConfigureAwait(false);

		return count > 0;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		_semaphore.Dispose();
	}

	/// <inheritdoc />
	public async Task<bool> ExistsAsync(Guid id, CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		return await _explorerItemRepository
			.ExistsAsync(x => x.Id == id, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<FileEntity[]> GetAllFilesAsync(
		OptionalFileProperties optionalProperties,
		CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		return await _fileRepository
			.GetAllAsync(optionalProperties, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<FolderEntity[]> GetAllFoldersAsync(CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		return await _folderRepository
			.GetAllAsync(token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public string GetDbFilePath() => _dbContextService.GetDbFilePath();

	/// <inheritdoc />
	public async Task<ValidatedContents> GetFileContentsAsync(Guid id, CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		if (await _fileRepository
			.GetContentsAsync(id, token)
			.ConfigureAwait(false) is not { } contents)
		{
			return new();
		}

		return new()
		{
			Contents = contents,
			Id = id,
			IsValid = true
		};
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<ValidatedContents> GetFileContentsRangeAsync(
		IEnumerable<Guid> ids,
		[EnumeratorCancellation] CancellationToken token = default)
	{
		await foreach (Guid id in ids.ToAsyncEnumerable())
		{
			yield return await GetFileContentsAsync(id, token).ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public async Task<string?> GetFileEditorStateAsync(Guid id, CancellationToken token = default)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		return await _fileRepository
			.GetEditorStateAsync(id, token)
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public bool IsValidSqliteDatabase(string databaseFilePath)
	{
		try
		{
			if (!HasValidHeader(databaseFilePath))
			{
				return false;
			}

			string connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = databaseFilePath
			}.ToString();

			using SqliteConnection connection = new(connectionString);

			SqlitePragmas.OpenConnection(connection);

			using SqliteCommand command = connection.CreateCommand();

			command.CommandText = "PRAGMA quick_check;";

			string? result = command
				.ExecuteScalar()?
				.ToString();

			SqliteConnection.ClearPool(connection);

			return string.Equals(
				result,
				"ok",
				StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}

		bool HasValidHeader(string filePath)
		{
			try
			{
				Span<byte> header = stackalloc byte[16];

				using Stream stream = _fileSystem.OpenRead(filePath);

				// Stryker disable once Equality : no SQLite file is as short as its header
				if (stream.Length < 16)
				{
					return false;
				}

				stream.ReadExactly(header);

				return header.StartsWith("SQLite format 3"u8);
			}
			catch
			{
				return false;
			}
		}
	}

	/// <inheritdoc />
	public LoadedEntities LoadEntities(string databaseFilePath)
	{
		using SqliteDbContext context = CreateSqliteDbContext(databaseFilePath);

		FolderEntity[] folders = [.. context
			.Set<FolderEntity>()
			.AsNoTracking()];

		FileEntity[] files = [.. context
			.Set<FileEntity>()
			.AsNoTracking()];

		ClearPool(context);

		return new()
		{
			Files = files,
			Folders = folders
		};
	}

	/// <inheritdoc />
	public async Task<bool> RenameAsync(
		EntityKind kind,
		Guid id,
		string name,
		DateTime updatedAt,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateItemAsync(kind, id, SetName, SetName, token).ConfigureAwait(false);

		void SetName<T>(UpdateSettersBuilder<T> builder) where T : ExplorerItemBase
		{
			builder
				.SetProperty(x => x.Name, name)
				.SetProperty(x => x.UpdatedAt, updatedAt);
		}
	}

	/// <inheritdoc />
	public async Task<HotkeyEntity[]> ReplaceHotkeysAsync(
		Guid fileId,
		KeyStroke[] hotkeys,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		try
		{
			HotkeyEntity[] entities = [.. ToHotkeyEntities(hotkeys, fileId)];

			await _dbContextService.ExecuteInTransactionAsync(async innerToken =>
			{
				await _hotkeysRepository
					.RemoveRangeByOwnerIdAsync(fileId, innerToken)
					.ConfigureAwait(false);

				foreach (HotkeyEntity item in entities)
				{
					await _hotkeysRepository
						.AddAsync(item, innerToken)
						.ConfigureAwait(false);
				}

				await _dbContextService
					.SaveChangesAsync(innerToken)
					.ConfigureAwait(false);
			}, token).ConfigureAwait(false);

			return entities;
		}
		finally
		{
			_dbContextService.ClearTracking();
		}
	}

	/// <inheritdoc />
	public async Task RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		DbConnection connection = _dbContextService.GetDbConnection();

		// Stryker disable all : the closed connection and the pool flags show only under pool load
		if (connection.State != ConnectionState.Closed)
		{
			connection.Close();
		}

		CopyDatabaseParameters parameters = new()
		{
			ClearDestinationPool = false,
			ClearSourcePool = true,
			DestinationFilePath = GetDbFilePath(),
			SourceFilePath = backupFilePath
		};
		// Stryker restore all

		CopyDatabase(parameters);
	}

	/// <inheritdoc />
	public async Task<bool> UpdateEncryptedDekAsync(
		Guid folderId,
		byte[] encryptedDek,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateFolderAsync(
			folderId,
			x => x.SetProperty(x => x.EncryptedDek, encryptedDek),
			token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFileContentsAsync(
		Guid fileId,
		byte[] contents,
		DateTime? updatedAt,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateFileAsync(fileId, SetContents, token).ConfigureAwait(false);

		void SetContents(UpdateSettersBuilder<FileEntity> builder)
		{
			builder.SetProperty(x => x.Contents, contents);

			if (updatedAt is { } date)
			{
				builder.SetProperty(x => x.UpdatedAt, date);
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFileEditorStateAsync(
		Guid fileId,
		string editorState,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateFileAsync(
			fileId,
			x => x.SetProperty(x => x.EditorState, editorState),
			token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<bool> UpdateIsExpandedAsync(
		Guid folderId,
		bool isExpanded,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateFolderAsync(
			folderId,
			x => x.SetProperty(x => x.IsExpanded, isExpanded),
			token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<bool> UpdateIsFavoriteAsync(
		Guid fileId,
		bool isFavorite,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateFileAsync(
			fileId,
			x => x.SetProperty(x => x.IsFavorite, isFavorite),
			token).ConfigureAwait(false);
	}

	/// <inheritdoc />
	public async Task<bool> UpdateIsSelectedAsync(
		EntityKind kind,
		Guid id,
		bool isSelected,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateItemAsync(kind, id, SetIsSelected, SetIsSelected, token).ConfigureAwait(false);

		void SetIsSelected<T>(UpdateSettersBuilder<T> builder) where T : ExplorerItemBase
		{
			builder.SetProperty(x => x.IsSelected, isSelected);
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateNoteAsync(
		EntityKind kind,
		Guid id,
		byte[]? note,
		DateTime updatedAt,
		CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		return await UpdateItemAsync(kind, id, SetNote, SetNote, token).ConfigureAwait(false);

		void SetNote<T>(UpdateSettersBuilder<T> builder) where T : ExplorerItemBase
		{
			builder
				.SetProperty(x => x.Note, note)
				.SetProperty(x => x.UpdatedAt, updatedAt);
		}
	}

	/// <inheritdoc />
	public async Task UpdateProtectionAsync(ProtectionUpdate update, CancellationToken token = default)
	{
		ThrowIfWriteRefused();

		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		await _dbContextService.ExecuteInTransactionAsync(async innerToken =>
		{
			foreach ((Guid fileId, byte[] contents) in update.Contents)
			{
				await _fileRepository
					.UpdatePropertiesAsync(fileId, [x => SetFile(x, fileId, contents)], innerToken)
					.ConfigureAwait(false);
			}

			// The folder of the key keeps its place among the folders whose notes change.
			foreach (Guid folderId in update.FolderNotes.Keys.Append(update.FolderId).Distinct())
			{
				await _folderRepository
					.UpdatePropertiesAsync(folderId, [x => SetFolder(x, folderId)], innerToken)
					.ConfigureAwait(false);
			}
		}, token).ConfigureAwait(false);

		void SetFile(UpdateSettersBuilder<FileEntity> builder, Guid fileId, byte[] contents)
		{
			builder
				.SetProperty(x => x.Contents, contents)
				.SetProperty(x => x.UpdatedAt, update.UpdatedAt);

			if (update.FileNotes.TryGetValue(fileId, out byte[]? note))
			{
				builder.SetProperty(x => x.Note, note);
			}
		}

		void SetFolder(UpdateSettersBuilder<FolderEntity> builder, Guid folderId)
		{
			if (update.FolderNotes.TryGetValue(folderId, out byte[]? note))
			{
				builder
					.SetProperty(x => x.Note, note)
					.SetProperty(x => x.UpdatedAt, update.UpdatedAt);
			}

			if (folderId == update.FolderId)
			{
				builder.SetProperty(x => x.EncryptedDek, update.EncryptedDek);
			}
		}
	}
	#endregion

	#region Helpers
	/// <inheritdoc cref="SqliteConnection.ClearPool" />
	private static void ClearPool(SqliteDbContext context)
	{
		using SqliteConnection connection = (SqliteConnection)context
			.Database
			.GetDbConnection();

		SqliteConnection.ClearPool(connection);
	}

	/// <summary>
	/// Copies one database file onto another through the SQLite backup API.
	/// </summary>
	private static void CopyDatabase(CopyDatabaseParameters parameters)
	{
		SqliteConnectionStringBuilder sourceBuilder = new()
		{
			DataSource = parameters.SourceFilePath
		};

		SqliteConnectionStringBuilder destinationBuilder = new()
		{
			DataSource = parameters.DestinationFilePath
		};

		using SqliteConnection source = new(sourceBuilder.ToString());

		using SqliteConnection destination = new(destinationBuilder.ToString());

		SqlitePragmas.OpenConnection(source);

		SqlitePragmas.OpenConnection(destination);

		source.BackupDatabase(destination);

		if (parameters.ClearSourcePool)
		{
			SqliteConnection.ClearPool(source);
		}

		if (parameters.ClearDestinationPool)
		{
			SqliteConnection.ClearPool(destination);
		}
	}

	/// <summary>
	/// Creates and returns <see cref="SqliteDbContext" />.
	/// </summary>
	private static SqliteDbContext CreateSqliteDbContext(string databaseFilePath)
	{
		SqliteConnectionStringBuilder builder = new()
		{
			DataSource = databaseFilePath
		};

		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseSqlite(builder.ToString())
			.AddInterceptors(new SqlitePragmaInterceptor())
			.Options;

		return new(options);
	}

	/// <summary>
	/// <c>True</c> when an object of <paramref name="kind" /> is kept with the folders, <c>false</c> when with the files.
	/// </summary>
	private static bool IsFolder(EntityKind kind) => kind switch
	{
		EntityKind.Folder => true,
		EntityKind.File or EntityKind.Dataset => false,
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	/// <summary>
	/// Transforms a sequence of <see cref="KeyStroke" /> to a sequence of <see cref="HotkeyEntity" />.
	/// </summary>
	private static IEnumerable<HotkeyEntity> ToHotkeyEntities(KeyStroke[] hotkeys, Guid ownerId)
	{
		for (int i = 0; i < hotkeys.Length; i++)
		{
			KeyStroke hotkey = hotkeys[i];

			yield return new()
			{
				Code = hotkey.Code,
				Id = Guid.NewGuid(),
				Index = i,
				Mask = hotkey.Mask,
				OwnerId = ownerId
			};
		}
	}

	/// <summary>
	/// Adds an <see cref="FileEntity" /> to the database.
	/// </summary>
	private async Task<FileEntity> AddFileAsync(
		AddEntityParameters parameters,
		CancellationToken token)
	{
		DateTime now = _timeProvider
			.GetLocalNow()
			.LocalDateTime;

		FileEntity file = new()
		{
			Contents = parameters.FileContents.AsNotNull(),
			CreatedAt = now,
			Id = Guid.NewGuid(),
			Index = parameters.Index,
			Kind = parameters.Kind,
			Name = parameters.Name,
			ParentId = parameters.ParentId,
			UpdatedAt = now
		};

		await _fileRepository
			.AddAsync(file, token)
			.ConfigureAwait(false);

		return file;
	}

	/// <summary>
	/// Adds an <see cref="FolderEntity" /> to the database.
	/// </summary>
	private async Task<FolderEntity> AddFolderAsync(
		AddEntityParameters parameters,
		CancellationToken token)
	{
		DateTime now = _timeProvider
			.GetLocalNow()
			.LocalDateTime;

		FolderEntity folder = new()
		{
			Id = Guid.NewGuid(),
			CreatedAt = now,
			Kind = parameters.Kind,
			Index = parameters.Index,
			Name = parameters.Name,
			ParentId = parameters.ParentId,
			UpdatedAt = now
		};

		await _folderRepository
			.AddAsync(folder, token)
			.ConfigureAwait(false);

		return folder;
	}

	/// <summary>
	/// Removes a file with its hotkeys; returns the number of files removed.
	/// </summary>
	private async Task<int> DeleteFileWithHotkeysAsync(Guid id, CancellationToken token)
	{
		await _hotkeysRepository
			.RemoveRangeByOwnerIdAsync(id, token)
			.ConfigureAwait(false);

		return await _fileRepository
			.RemoveAsync(id, token)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Removes a folder with everything in it; returns the number of folders removed.
	/// </summary>
	private async Task<int> DeleteFolderTreeAsync(Guid id, CancellationToken token)
	{
		Guid[] folderIds = await _folderRepository
			.GetFolderSubtreeIdsAsync(id, token)
			.ToArrayAsync(token)
			.ConfigureAwait(false);

		Guid[] fileIds = await _fileRepository
			.GetFileIdsAsync(folderIds, token)
			.ConfigureAwait(false);

		if (fileIds.Length > 0)
		{
			await _hotkeysRepository
				.RemoveRangeByOwnerIdsAsync(fileIds, token)
				.ConfigureAwait(false);

			await _fileRepository
				.RemoveRangeByIdsAsync(fileIds, token)
				.ConfigureAwait(false);
		}

		return await _folderRepository
			.RemoveRangeByIdsAsync(folderIds, token)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Tells an existing database that cannot be opened from one whose schema does not match this version.
	/// </summary>
	private async Task<DbConnectionStatus> GetSchemaStatusAsync(CancellationToken token)
	{
		if (!IsValidSqliteDatabase(_dbContextService.GetDbFilePath())
			|| !await _dbContextService
				.CanConnectAsync(token)
				.ConfigureAwait(false))
		{
			return DbConnectionStatus.FileUnreadable;
		}

		if (!_dbContextService.HasMigrations())
		{
			return DbConnectionStatus.Connected;
		}

		IEnumerable<string> appliedMigrations = await _dbContextService
			.GetAppliedMigrationsAsync(token)
			.ConfigureAwait(false);

		bool isFromNewerVersion = appliedMigrations
			.Except(_dbContextService.GetKnownMigrations())
			.Any();

		// A schema older than this version is left to the migration itself: it fails on the tables
		// that are already there, and a failure on a database that reads is about its schema.
		return isFromNewerVersion
			? DbConnectionStatus.SchemaTooNew
			: DbConnectionStatus.Connected;
	}

	/// <summary>
	/// Refuses a change when the database is closed for writing.
	/// </summary>
	private void ThrowIfWriteRefused([CallerMemberName] string callerName = "")
	{
		if (IsWritable)
		{
			return;
		}

		throw new DatabaseNotWritableException(ConnectionStatus, callerName);
	}

	/// <summary>
	/// Erases the free pages of the database once, keeping a failure to the log.
	/// </summary>
	private async Task TryEraseFreePagesOnceAsync(CancellationToken token)
	{
		try
		{
			await _dbMaintenance
				.EraseFreePagesOnceAsync(token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Erases the copies of the database left by an interrupted conversion, keeping a failure to the log.
	/// </summary>
	private void TryErasePendingBackups()
	{
		try
		{
			_dbMaintenance.ErasePendingBackups();
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Brings the schema to the model, reporting <paramref name="failureStatus" /> when that cannot be done.
	/// </summary>
	private async Task<DbConnectionStatus> TryUpdateSchemaAsync(
		DbConnectionStatus failureStatus,
		CancellationToken token)
	{
		try
		{
			await (_dbContextService.HasMigrations()
				? _dbContextService.MigrateAsync(token)
				: _dbContextService.EnsureCreatedAsync(token)).ConfigureAwait(false);

			return DbConnectionStatus.Connected;
		}
		catch (OperationCanceledException)
		{
			// A cancelled connect is the caller giving up, not a schema that cannot be updated.
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return failureStatus;
		}
	}

	/// <summary>
	/// Writes to a file; <c>false</c> when there is no such file.
	/// </summary>
	private async Task<bool> UpdateFileAsync(
		Guid id,
		Action<UpdateSettersBuilder<FileEntity>> setter,
		CancellationToken token)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		int count = await _fileRepository
			.UpdatePropertiesAsync(id, [setter], token)
			.ConfigureAwait(false);

		return count > 0;
	}

	/// <summary>
	/// Writes to a folder; <c>false</c> when there is no such folder.
	/// </summary>
	private async Task<bool> UpdateFolderAsync(
		Guid id,
		Action<UpdateSettersBuilder<FolderEntity>> setter,
		CancellationToken token)
	{
		using SemaphoreLock held = await _semaphore
			.LockAsync(token)
			.ConfigureAwait(false);

		int count = await _folderRepository
			.UpdatePropertiesAsync(id, [setter], token)
			.ConfigureAwait(false);

		return count > 0;
	}

	/// <summary>
	/// Writes to the folder or the file, whichever <paramref name="kind" /> keeps the object with; the caller passes one
	/// setter for each.
	/// </summary>
	private Task<bool> UpdateItemAsync(
		EntityKind kind,
		Guid id,
		Action<UpdateSettersBuilder<FolderEntity>> folderSetter,
		Action<UpdateSettersBuilder<FileEntity>> fileSetter,
		CancellationToken token)
	{
		return IsFolder(kind)
			? UpdateFolderAsync(id, folderSetter, token)
			: UpdateFileAsync(id, fileSetter, token);
	}
	#endregion
}
