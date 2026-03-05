using Entities.Abstract;
using Entities.Enums;
using Entities.Interfaces;
using Entities.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repository.DbContexts;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class DbAccess : IDbAccess
{
	#region Data
	/// <inheritdoc cref="IExplorerModelBaseRepository" />
	private readonly IExplorerModelBaseRepository _baseRepository;

	/// <inheritdoc cref="SqliteDbContext" />
	private readonly SqliteDbContext _dbContext;

	/// <inheritdoc cref="IDbContextService" />
	private readonly IDbContextService _dbContextService;

	/// <inheritdoc cref="IFoldersRepository" />
	private readonly IFilesRepository _filesRepository;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IFoldersRepository" />
	private readonly IFoldersRepository _foldersRepository;

	/// <inheritdoc cref="IHotkeysRepository" />
	private readonly IHotkeysRepository _hotkeysRepository;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="SemaphoreSlim" />
	private readonly SemaphoreSlim _semaphore = new(1, 1);
	#endregion

	#region Constructors
	public DbAccess(
		IDbContextService dbContextService,
		IExplorerModelBaseRepository baseRepository,
		IFilesRepository filesRepository,
		IFileSystem fileSystem,
		IFoldersRepository foldersRepository,
		IHotkeysRepository hotkeysRepository,
		ILogger logger,
		SqliteDbContext dbContext)
	{
		_baseRepository = baseRepository;

		_dbContext = dbContext;

		_dbContextService = dbContextService;

		_filesRepository = filesRepository;

		_fileSystem = fileSystem;

		_foldersRepository = foldersRepository;

		_hotkeysRepository = hotkeysRepository;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<ExplorerModelBase?> AddEntityAsync(
		AddEntityParameters parameters,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			ExplorerModelBase entity = parameters.EntityType == EntityType.Folder
				? await AddFolderAsync(parameters, token).ConfigureAwait(false)
				: await AddFileAsync(parameters, token).ConfigureAwait(false);

			await _dbContext
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<HotkeyModel[]> AddHotkeysAsync(
		Guid fileId,
		CodeMaskPair[] hotkeys,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			HotkeyModel[] entities = [.. hotkeys.Select(x => new HotkeyModel
			{
				Code = x.Code,
				Id = Guid.NewGuid(),
				Mask = x.Mask,
				OwnerId = fileId
			})];

			// Hot keys must be added sequentially and saved, otherwise their order in the database will be disrupted.
			foreach (HotkeyModel item in entities)
			{
				await _hotkeysRepository
					.AddAsync(item, token)
					.ConfigureAwait(false);

				await _dbContext
					.SaveChangesAsync(token)
					.ConfigureAwait(false);
			}

			return entities;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public string? BackupDatabase()
	{
		try
		{
			string dbFilePath = GetDbFilePath();

			if (!_fileSystem.IsFileExists(dbFilePath) || Path.GetDirectoryName(dbFilePath) is not { } directory)
			{
				return null;
			}

			string backupPath = Path.Combine(directory, "Backup" + AppUtils.SQLiteExtension);

			BackupSqliteDatabase(dbFilePath, backupPath);

			if (!_fileSystem.IsFileExists(backupPath))
			{
				return null;
			}

			return backupPath;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
	}

	/// <inheritdoc />
	public void BackupSqliteDatabase(string sourceFilePath, string destFilePath)
	{
		SqliteConnectionStringBuilder sourceBuilder = new()
		{
			DataSource = sourceFilePath
		};

		SqliteConnectionStringBuilder destBuilder = new()
		{
			DataSource = destFilePath
		};

		using SqliteConnection source = new(sourceBuilder.ToString());

		using SqliteConnection destination = new(destBuilder.ToString());

		source.Open();

		destination.Open();

		source.BackupDatabase(destination);
	}

	/// <inheritdoc />
	public void BackupSqliteDatabase(in BackupSqliteParameters parameters)
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

		source.Open();

		dest.Open();

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

	/// <inheritdoc />
	public Task ConnectAsync(
		bool useMigrations,
		CancellationToken token = default)
	{
		try
		{
			_logger.LogInformation("Connecting to the database.");

			return useMigrations && _dbContextService.HasMigrations(Assembly.GetExecutingAssembly())
				? _dbContextService.MigrateAsync(token)
				: _dbContextService.EnsureCreatedAsync(token);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return Task.CompletedTask;
		}
	}

	/// <inheritdoc />
	public async Task<int> CountOfAsync(
		Expression<Func<ExplorerModelBase, bool>> condition,
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteFileAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			DetachAndDelete(await _filesRepository
				.GetAsync(id, includeDependencies: true, token: token)
				.ConfigureAwait(false));

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteFolderAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			DetachAndDelete(await GetChildFilesAsync(id, includeDependencies: true, token)
				.ToArrayAsync(token)
				.ConfigureAwait(false));

			DetachAndDelete(await GetChildFoldersAsync(id, token)
				.ToArrayAsync(token)
				.ConfigureAwait(false));

			DetachAndDelete(await _foldersRepository
				.GetAsync(id, token: token)
				.ConfigureAwait(false));

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> DeleteHotkeysAsync(Guid fileId, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			HotkeyModel[] hotkeys = await _hotkeysRepository
				.GetAsync(x => x.OwnerId == fileId, token: token)
				.ConfigureAwait(false);

			DetachAndDelete(hotkeys);

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public void Dispose() => _dbContext.Dispose();

	/// <inheritdoc />
	public async Task<FileModel[]> GetAllFilesAsync(
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default,
		params string[] excludedProperties)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _filesRepository.GetAllAsync(
				includeDependencies,
				trackChanges,
				token,
				excludedProperties).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<FolderModel[]> GetAllFoldersAsync(
		bool trackChanges = false,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			return await _foldersRepository
				.GetAllAsync(trackChanges, token)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return [];
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public string GetDbFilePath()
	{
		return _dbContext
			.Database
			.GetDbConnection()
			.DataSource;
	}

	/// <inheritdoc />
	public async Task<ContentsIsValidPair> GetFileContentsAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			FileModel entity = await _filesRepository
				.GetAsync(id, token: token)
				.ConfigureAwait(false);

			return new()
			{
				Contents = entity.Contents,
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<string?> GetFilePropertiesAsync(Guid id, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			FileModel entity = await _filesRepository
				.GetAsync(id, token: token)
				.ConfigureAwait(false);

			return entity.Properties;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			return null;
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<ContentsIsValidPair> GetFilesContentsAsync(
		IEnumerable<Guid> identifiers,
		[EnumeratorCancellation] CancellationToken token = default)
	{
		await foreach (Guid id in identifiers.ToAsyncEnumerable())
		{
			yield return await GetFileContentsAsync(id, token).ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public async Task RestoreFromBackupAsync(string backupFilePath, CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			DbConnection connection = _dbContext
				.Database
				.GetDbConnection();

			if (connection.State != ConnectionState.Closed)
			{
				connection.Close();
			}

			BackupSqliteDatabase(backupFilePath, GetDbFilePath());
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
		finally
		{
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdatePropertiesAsync<T>(
		T dtoSource,
		CancellationToken token,
		params string[] propertyNames) where T : class, IIdentity
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			ExplorerModelBase entity = await _baseRepository
				.GetAsync(dtoSource.Id, trackChanges: true, token)
				.ConfigureAwait(false);

			dtoSource.CopyPropertiesTo(entity, propertyNames);

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdatePropertiesAsync(
		Guid id,
		CancellationToken token,
		params PropertyNameValuePair[] properties)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			ExplorerModelBase entity = await _baseRepository
				.GetAsync(id, trackChanges: true, token)
				.ConfigureAwait(false);

			properties.ForEach(x => entity.SetPropertyValue(x.PropertyName, x.Value));

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdatePropertiesAsync(
		IDictionary<Guid, PropertyNameValuePair[]> relations,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			foreach (KeyValuePair<Guid, PropertyNameValuePair[]> relation in relations)
			{
				ExplorerModelBase entity = await _baseRepository
					.GetAsync(relation.Key, trackChanges: true, token)
					.ConfigureAwait(false);

				relation
					.Value
					.ForEach(x => entity.SetPropertyValue(x.PropertyName, x.Value));
			}

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdatePropertyAsync<T>(
		Guid id,
		string propertyName,
		T value,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			ExplorerModelBase entity = await _baseRepository
				.GetAsync(id, trackChanges: true, token)
				.ConfigureAwait(false);

			entity.SetPropertyValue(propertyName, value);

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}

	/// <inheritdoc />
	public async Task<bool> UpdatePropertyAsync<T>(
		IEnumerable<Guid> identifiers,
		string propertyName,
		T value,
		CancellationToken token = default)
	{
		try
		{
			await _semaphore
				.WaitAsync(token)
				.ConfigureAwait(false);

			ExplorerModelBase[] sequence = await _baseRepository
				.GetAsync(identifiers, trackChanges: true, token)
				.ConfigureAwait(false);

			sequence.ForEach(x => x.SetPropertyValue(propertyName, value));

			int count = await _dbContext
				.SaveChangesAsync(token)
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
			_semaphore.Release();
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Adds an <see cref="FileModel" /> to the database.
	/// </summary>
	private async Task<FileModel> AddFileAsync(
		AddEntityParameters parameters,
		CancellationToken token)
	{
		DateTime now = DateTime.Now;

		FileModel file = new()
		{
			Contents = parameters.FileContents.AsNotNull(),
			CreatedDate = now,
			EntityType = parameters.EntityType,
			Id = Guid.NewGuid(),
			Index = parameters.Index,
			Name = parameters.Name,
			ParentId = parameters.ParentId,
			UpdatedDate = now
		};

		await _filesRepository
			.AddAsync(file, token)
			.ConfigureAwait(false);

		return file;
	}

	/// <summary>
	/// Adds an <see cref="FolderModel" /> to the database.
	/// </summary>
	private async Task<FolderModel> AddFolderAsync(
		AddEntityParameters parameters,
		CancellationToken token)
	{
		DateTime now = DateTime.Now;

		FolderModel folder = new()
		{
			Id = Guid.NewGuid(),
			CreatedDate = now,
			EntityType = parameters.EntityType,
			Index = parameters.Index,
			Name = parameters.Name,
			ParentId = parameters.ParentId,
			UpdatedDate = now
		};

		await _foldersRepository
			.AddAsync(folder, token)
			.ConfigureAwait(false);

		return folder;
	}

	/// <summary>
	/// Stops tracking changes and deletes <see cref="FolderModel" />.
	/// </summary>
	private void DetachAndDelete(FolderModel entity)
	{
		_dbContextService.Detach<FolderModel>(entity.Id);

		_foldersRepository.Remove(entity);
	}

	/// <summary>
	/// Stops tracking changes and deletes <see cref="FileModel" />.
	/// </summary>
	private void DetachAndDelete(FileModel file)
	{
		_dbContextService.Detach<FileModel>(file.Id);

		_filesRepository.Remove(file);
	}

	/// <summary>
	/// Stops tracking changes and deletes sequence of <see cref="FolderModel" />.
	/// </summary>
	private void DetachAndDelete(FolderModel[] folders)
	{
		if (folders.Length == 0)
		{
			return;
		}

		_dbContextService.Detach(folders);

		_foldersRepository.RemoveRange(folders);
	}

	/// <summary>
	/// Stops tracking changes and deletes sequence of <see cref="FileModel" />.
	/// </summary>
	private void DetachAndDelete(FileModel[] files)
	{
		if (files.Length == 0)
		{
			return;
		}

		_dbContextService.Detach(files);

		_filesRepository.RemoveRange(files);
	}

	/// <summary>
	/// Stops tracking changes and deletes sequence of <see cref="FileModel" />.
	/// </summary>
	private void DetachAndDelete(HotkeyModel[] hotkeys)
	{
		if (hotkeys.Length == 0)
		{
			return;
		}

		_dbContextService.Detach(hotkeys);

		_hotkeysRepository.RemoveRange(hotkeys);
	}

	/// <summary>
	/// Returns a flat list of child <see cref="FileModel" /> objects according to parent ID, recursively.
	/// </summary>
	private async IAsyncEnumerable<FileModel> GetChildFilesAsync(
		Guid parentId,
		bool includeDependencies = false,
		[EnumeratorCancellation] CancellationToken token = default)
	{
		foreach (FileModel document in await _filesRepository
			.GetAsync(x => x.ParentId == parentId, includeDependencies, token: token)
			.ConfigureAwait(false))
		{
			yield return document;
		}

		foreach (FolderModel folder in await _foldersRepository
			.GetAsync(x => x.ParentId == parentId, token: token)
			.ConfigureAwait(false))
		{
			await foreach (FileModel child in GetChildFilesAsync(
				folder.Id,
				includeDependencies,
				token).ConfigureAwait(false))
			{
				yield return child;
			}
		}
	}

	/// <summary>
	/// Returns a flat list of child <see cref="FolderModel" /> entities according to parent ID, recursively.
	/// </summary>
	private async IAsyncEnumerable<FolderModel> GetChildFoldersAsync(
		Guid parentId,
		[EnumeratorCancellation] CancellationToken token)
	{
		foreach (FolderModel child in await _foldersRepository
			.GetAsync(x => x.ParentId == parentId, token: token)
			.ConfigureAwait(false))
		{
			yield return child;

			await foreach (FolderModel item in GetChildFoldersAsync(child.Id, token).ConfigureAwait(false))
			{
				yield return item;
			}
		}
	}
	#endregion
}
