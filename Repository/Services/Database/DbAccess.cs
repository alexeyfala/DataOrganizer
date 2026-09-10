using Entities.Enums;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.DbContexts;
using Repository.Dto;
using Repository.Enums;
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

namespace Repository.Services.Database;

public sealed class DbAccess : IDbAccess
{
	#region Properties
	/// <inheritdoc />
	public bool IsWritable => Status is DbConnectionStatus.Connected;

	/// <inheritdoc />
	public DbConnectionStatus Status { get; private set; } = DbConnectionStatus.Connected;
	#endregion

	#region Data
	/// <inheritdoc cref="IExplorerItemRepository" />
	private readonly IExplorerItemRepository _baseRepository;

	/// <inheritdoc cref="IDbContextService" />
	private readonly IDbContextService _dbContextService;

	/// <inheritdoc cref="IDbMaintenance" />
	private readonly IDbMaintenance _dbMaintenance;

	/// <inheritdoc cref="IFolderRepository" />
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

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public DbAccess(
		IDbContextService dbContextService,
		IDbMaintenance dbMaintenance,
		IExplorerItemRepository baseRepository,
		IFileRepository fileRepository,
		IFileSystem fileSystem,
		IFolderRepository folderRepository,
		IHotkeysRepository hotkeysRepository,
		ILogger logger)
	{
		_baseRepository = baseRepository;

		_dbContextService = dbContextService;

		_dbMaintenance = dbMaintenance;

		_fileRepository = fileRepository;

		_fileSystem = fileSystem;

		_folderRepository = folderRepository;

		_hotkeysRepository = hotkeysRepository;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ExplorerItemBase?> AddEntityAsync(
		AddEntityParameters parameters,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return null;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			ExplorerItemBase entity = parameters.Kind == EntityKind.Folder
				? await AddFolderAsync(parameters, token).ConfigureAwait(false)
				: await AddFileAsync(parameters, token).ConfigureAwait(false);

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);

			return entity;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> AddFilesAsync(IEnumerable<FileEntity> files, CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			await _fileRepository
				.AddRangeAsync(files, token)
				.ConfigureAwait(false);

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> AddFoldersAsync(IEnumerable<FolderEntity> folders, CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			await _folderRepository
				.AddRangeAsync(folders, token)
				.ConfigureAwait(false);

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<HotkeyEntity[]> AddHotkeysAsync(
		Guid fileId,
		KeyStroke[] hotkeys,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return [];
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			HotkeyEntity[] entities = [.. ToHotkeyEntities(hotkeys, fileId)];

			foreach (HotkeyEntity item in entities)
			{
				await _hotkeysRepository
					.AddAsync(item, token)
					.ConfigureAwait(false);
			}

			await _dbContextService
				.SaveChangesAsync(token)
				.ConfigureAwait(false);

			return entities;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<DatabaseBackup?> BackupDatabaseAsync(CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			string dbFilePath = GetDbFilePath();

			if (!_fileSystem.IsFileExists(dbFilePath) || Path.GetDirectoryName(dbFilePath) is not { })
			{
				return null;
			}

			string backupFilePath = DatabaseBackup.CreateFilePath(dbFilePath);

			_fileSystem.CreateDirectory(DatabaseBackup.GetDirectoryPath(dbFilePath));

			BackupSqliteParameters parameters = new()
			{
				ClearDestPool = true,
				ClearSourcePool = false,
				DestFilePath = backupFilePath,
				SourceFilePath = dbFilePath
			};

			BackupSqliteDatabase(parameters);

			if (!_fileSystem.IsFileExists(backupFilePath))
			{
				return null;
			}

			return new DatabaseBackup(
				backupFilePath,
				_fileSystem,
				_logger);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task BackupSqliteDatabaseAsync(
		BackupSqliteParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			BackupSqliteDatabase(parameters);
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> ClearDatabaseAsync(CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
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

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<DbConnectionStatus> ConnectAsync(CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			_logger.LogInformation("Connecting to the database.");

			TryErasePendingBackups();

			bool isExisting = _fileSystem.IsFileExists(_dbContextService.GetDbFilePath());

			DbConnectionStatus status = isExisting
				? await GetSchemaStatusAsync(token).ConfigureAwait(false)
				: DbConnectionStatus.Connected;

			if (status is DbConnectionStatus.Connected)
			{
				DbConnectionStatus failure = isExisting
					? DbConnectionStatus.SchemaTooOld
					: DbConnectionStatus.FileUnreadable;

				// A database that has just been read holds data, so a failure here is about its schema.
				status = await TryUpdateSchemaAsync(failure, token).ConfigureAwait(false);
			}

			Status = status;

			if (status is not DbConnectionStatus.Connected)
			{
				_logger.LogError($"The database cannot be worked with: {status}.", assertDebug: false);

				return status;
			}

			await TryEraseFreePagesOnceAsync(token).ConfigureAwait(false);

			return status;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			Status = DbConnectionStatus.FileUnreadable;

			return Status;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<int> CountOfAsync(
		Expression<Func<ExplorerItemBase, bool>> condition,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _baseRepository
				.CountOfAsync(condition, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return default;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteFileAsync(Guid id, CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			await _hotkeysRepository
				.RemoveRangeByOwnerIdAsync(id, token)
				.ConfigureAwait(false);

			int count = await _fileRepository
				.RemoveAsync(id, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteFolderAsync(Guid id, CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

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

			int count = await _folderRepository
				.RemoveRangeByIdsAsync(folderIds, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteHotkeysAsync(Guid fileId, CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			int count = await _hotkeysRepository
				.RemoveRangeByOwnerIdAsync(fileId, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
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
	public async Task<FileEntity[]> GetAllFilesAsync(
		OptionalFileProperties optionalProperties,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _fileRepository
				.GetAllAsync(optionalProperties, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<FolderEntity[]> GetAllFoldersAsync(CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _folderRepository
				.GetAllAsync(token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public string GetDbFilePath() => _dbContextService.GetDbFilePath();

	/// <inheritdoc />
	public async Task<ValidatedContents> GetFileContentsAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
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
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return new();
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<string?> GetFileEditorStateAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _fileRepository
				.GetEditorStateAsync(id, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<ValidatedContents> GetFilesContentsAsync(
		IEnumerable<Guid> identifiers,
		[EnumeratorCancellation] CancellationToken token = default)
	{
		await foreach (Guid id in identifiers.ToAsyncEnumerable())
		{
			yield return await GetFileContentsAsync(id, token).ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public async Task<bool> ExistsAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _baseRepository
				.ExistsAsync(x => x.Id == id, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public bool IsValidSQLiteDatabase(string dataSource, bool deepCheck = false)
	{
		try
		{
			if (!HasValidHeader(dataSource))
			{
				return false;
			}

			string connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = dataSource
			}.ToString();

			using SqliteConnection connection = new(connectionString);

			SqlitePragmas.Open(connection);

			using SqliteCommand cmd = connection.CreateCommand();

			cmd.CommandText = deepCheck
				? "PRAGMA integrity_check;"
				: "PRAGMA quick_check;";

			string? result = cmd
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
	public LoadedEntities LoadFromDb(string dataSource)
	{
		using SqliteDbContext context = GetSQliteDbContext(dataSource);

		FolderEntity[] dbFolders = [.. context
			.Set<FolderEntity>()
			.AsNoTracking()];

		FileEntity[] dbFiles = [.. context
			.Set<FileEntity>()
			.AsNoTracking()];

		ClearPool(context);

		return new()
		{
			Files = dbFiles,
			Folders = dbFolders
		};
	}

	/// <inheritdoc />
	public async Task<bool> RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			DbConnection connection = _dbContextService.GetDbConnection();

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}

			BackupSqliteParameters parameters = new()
			{
				ClearDestPool = false,
				ClearSourcePool = true,
				DestFilePath = GetDbFilePath(),
				SourceFilePath = backupFilePath
			};

			BackupSqliteDatabase(parameters);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFileAndFolderPropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> fileUpdates,
		IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> folderUpdates,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			await _dbContextService.ExecuteInTransactionAsync(async innerToken =>
			{
				foreach (KeyValuePair<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> update in fileUpdates)
				{
					await _fileRepository
						.UpdatePropertiesAsync(update.Key, update.Value, innerToken)
						.ConfigureAwait(false);
				}

				foreach (KeyValuePair<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> update in folderUpdates)
				{
					await _folderRepository
						.UpdatePropertiesAsync(update.Key, update.Value, innerToken)
						.ConfigureAwait(false);
				}
			}, token).ConfigureAwait(false);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFilePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FileEntity>>[] setters,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			int count = await _fileRepository
				.UpdatePropertiesAsync(id, setters, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFilePropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileEntity>>[]> updates,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			int count = await _fileRepository
				.UpdatePropertiesAsync(updates, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFolderPropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FolderEntity>>[] setters,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			int count = await _folderRepository
				.UpdatePropertiesAsync(id, setters, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdateFolderPropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FolderEntity>>[]> updates,
		CancellationToken token = default)
	{
		if (IsWriteRefused())
		{
			return false;
		}

		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			int count = await _folderRepository
				.UpdatePropertiesAsync(updates, token)
				.ConfigureAwait(false);

			return count > 0;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return false;
		}
		finally
		{
			try
			{
				_semaphore.Release();
			}
			catch (ObjectDisposedException)
			{
				// Service was disposed concurrently — safe to ignore.
			}
		}
	}
	#endregion

	#region Helpers
	/// <inheritdoc cref="BackupSqliteDatabaseAsync" />
	private static void BackupSqliteDatabase(BackupSqliteParameters parameters)
	{
		SqliteConnectionStringBuilder sourceBuilder = new()
		{
			DataSource = parameters.SourceFilePath
		};

		SqliteConnectionStringBuilder destBuilder = new()
		{
			DataSource = parameters.DestFilePath
		};

		using SqliteConnection source = new(sourceBuilder.ToString());

		using SqliteConnection dest = new(destBuilder.ToString());

		SqlitePragmas.Open(source);

		SqlitePragmas.Open(dest);

		source.BackupDatabase(dest);

		if (parameters.ClearSourcePool)
		{
			SqliteConnection.ClearPool(source);
		}

		if (parameters.ClearDestPool)
		{
			SqliteConnection.ClearPool(dest);
		}
	}

	/// <inheritdoc cref="SqliteConnection.ClearPool" />
	private static void ClearPool(SqliteDbContext context)
	{
		using SqliteConnection connection = (SqliteConnection)context
			.Database
			.GetDbConnection();

		SqliteConnection.ClearPool(connection);
	}

	/// <summary>
	/// Creates and returns <see cref="SqliteDbContext" />.
	/// </summary>
	private static SqliteDbContext GetSQliteDbContext(string dataSource)
	{
		SqliteConnectionStringBuilder builder = new()
		{
			DataSource = dataSource
		};

		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseSqlite(builder.ToString())
			.AddInterceptors(new SqlitePragmaInterceptor())
			.Options;

		return new(options);
	}

	/// <summary>
	/// Transforms a sequence of <see cref="KeyStroke" /> to a sequence of <see cref="HotkeyEntity" />.
	/// </summary>
	private static IEnumerable<HotkeyEntity> ToHotkeyEntities(KeyStroke[] sequence, Guid ownerId)
	{
		for (int i = 0; i < sequence.Length; i++)
		{
			KeyStroke x = sequence[i];

			yield return new()
			{
				Code = x.Code,
				Id = Guid.NewGuid(),
				Index = i,
				Mask = x.Mask,
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
		DateTime now = DateTime.Now;

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
		DateTime now = DateTime.Now;

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
	/// Tells an existing database that cannot be opened from one whose schema does not match this version.
	/// </summary>
	private async Task<DbConnectionStatus> GetSchemaStatusAsync(CancellationToken token)
	{
		if (!IsValidSQLiteDatabase(_dbContextService.GetDbFilePath())
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

		IEnumerable<string> applied = await _dbContextService
			.GetAppliedMigrationsAsync(token)
			.ConfigureAwait(false);

		bool isFromNewerVersion = applied
			.Except(_dbContextService.GetKnownMigrations())
			.Any();

		// A schema older than this version is left to the migration itself: it fails on the tables
		// that are already there, and a failure on a database that reads is about its schema.
		return isFromNewerVersion
			? DbConnectionStatus.SchemaTooNew
			: DbConnectionStatus.Connected;
	}

	/// <summary>
	/// <c>True</c> when the database is closed for writing; the refusal is kept to the log.
	/// </summary>
	private bool IsWriteRefused([CallerMemberName] string caller = "")
	{
		if (IsWritable)
		{
			return false;
		}

		_logger.LogError($"{caller} is refused: the database is {Status}.", assertDebug: false);

		return true;
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
	/// Brings the schema to the model, reporting <paramref name="failure" /> when that cannot be done.
	/// </summary>
	private async Task<DbConnectionStatus> TryUpdateSchemaAsync(
		DbConnectionStatus failure,
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
			_logger.LogException(ex, assertDebug: false);

			return failure;
		}
	}
	#endregion
}
