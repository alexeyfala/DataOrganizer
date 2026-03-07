using Avalonia.Platform.Storage;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using Entities.Abstract;
using Entities.Models;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class DataExchangeService : IDataExchangeService
{
	#region Data
	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializerWrapper" />
	private readonly IJsonSerializerWrapper _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IFileSystemEnrtyPicker" />
	private readonly IFileSystemEnrtyPicker _picker;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <inheritdoc cref="IViewModelExecutionService" />
	private readonly IViewModelExecutionService _viewModel;
	#endregion

	#region Constructors
	public DataExchangeService(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityLoader entityLoader,
		IFileSystem fileSystem,
		IFileSystemEnrtyPicker picker,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IViewFactory viewFactory,
		IViewModelExecutionService viewModel)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_picker = picker;

		_viewFactory = viewFactory;

		_viewModel = viewModel;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ExportDataAsync(CancellationToken token = default)
	{
		// TODO: Test
		FilePickerSaveOptions options = new()
		{
			DefaultExtension = IFileSystemEnrtyPicker.JsonExt.TrimStart('.'),
			FileTypeChoices = IFileSystemEnrtyPicker.ImportExportFilePickerTypes,
			ShowOverwritePrompt = true,
			SuggestedFileName = AppUtils.AppNameInOneWord,
			Title = Strings.SaveAs
		};

		if (await _picker
			.SaveFileAsync<EditorWindow>(options)
			.ConfigureAwait(false) is not { } filePath)
		{
			return;
		}

		try
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemEnrtyPicker.JsonExt:
					await ExportToJsonAsync(filePath, token).ConfigureAwait(false);
					break;

				case IFileSystemEnrtyPicker.XmlExt:
					break;

				case AppUtils.SQLiteExtension:
					ExportToSQLite(filePath);
					break;

				default:
					throw new NotImplementedException();
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToExportData));
		}
		finally
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = false);
		}
	}

	/// <inheritdoc />
	public async Task ImportDataAsync(
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		// TODO: Test
		ImportListVariant variant = ImportListVariant.Replace;

		if (hierarchy.Count != 0)
		{
			variant = await _dialogService
				.SelectImportVariantAsync(token)
				.ConfigureAwait(false);

			if (variant == ImportListVariant.None)
			{
				return;
			}
		}

		FilePickerOpenOptions options = new()
		{
			AllowMultiple = false,
			FileTypeFilter = IFileSystemEnrtyPicker.ImportExportFilePickerTypes,
			Title = Strings.Select
		};

		string[] filePaths = await _picker
			.SelectFilesAsync<EditorWindow>(options)
			.ConfigureAwait(false);

		if (filePaths.Length == 0)
		{
			return;
		}

		if (_dbAccess.BackupDatabase() is not { } backupFilePath)
		{
			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup));

			return;
		}

		try
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			// The path may contain %20 instead of spaces, so it needs to be decoded.
			string filePath = WebUtility.UrlDecode(filePaths[0]);

			List<ExplorerModelBaseDto> objects = [];

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemEnrtyPicker.JsonExt:
					if (!await ImportFromJsonAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token))
					{
						_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToImportData));

						await _dbAccess
							.RestoreFromBackupAsync(backupFilePath, token)
							.ConfigureAwait(false);

						return;
					}
					break;

				case IFileSystemEnrtyPicker.XmlExt:
					break;

				case AppUtils.SQLiteExtension:
					if (!_dbAccess.IsValidSQLiteDatabase(filePath) || !await ImportFromSQLiteAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token).ConfigureAwait(false))
					{
						_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToImportData));

						await _dbAccess
							.RestoreFromBackupAsync(backupFilePath, token)
							.ConfigureAwait(false);

						return;
					}
					break;

				default:
					throw new NotImplementedException();
			}

			_viewModel.ExecuteInEditor(x => x.AddHierarchy(objects));

			if (variant == ImportListVariant.Replace)
			{
				_viewModel.ExecuteInEditor(x => x
					.CopyHistorySettings
					.CopyHistory
					.Clear());
			}

			_viewModel.ExecuteInEditor(x => x.ShowInfoSnackbar(Strings.DataImportCompleted));
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToImportData));

			await _dbAccess
				.RestoreFromBackupAsync(backupFilePath, token)
				.ConfigureAwait(false);
		}
		finally
		{
			try
			{
				_fileSystem.EraseAndDeleteFile(backupFilePath);
			}
			catch (Exception ex)
			{
				_logger.LogException(ex);
			}

			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = false);
		}
	}
	#endregion

	#region Service
	/// <summary>
	/// Regenerates identifiers.
	/// </summary>
	private static void RegenerateId(FolderModel[] folders, FileModel[] files)
	{
		files.ForEach(file =>
		{
			Guid newFileId = Guid.NewGuid();

			file.Id = newFileId;

			file.Hotkeys.ForEach(hotkey =>
			{
				hotkey.Id = Guid.NewGuid();

				hotkey.OwnerId = newFileId;
			});
		});

		folders.ForEach(folder =>
		{
			FolderModel[] childFolders = [.. folders.Where(x => folder.Id == x.ParentId)];

			FileModel[] childFiles = [.. files.Where(x => folder.Id == x.ParentId)];

			Guid newFolderId = Guid.NewGuid();

			folder.Id = newFolderId;

			childFolders.ForEach(x => x.ParentId = newFolderId);

			childFiles.ForEach(x => x.ParentId = newFolderId);
		});
	}

	/// <summary>
	/// Adds a data to the list from JSON.
	/// </summary>
	private async Task<bool> AddToListFromJsonAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		return true;
	}

	/// <summary>
	/// Adds a data to the list from SQLite database.
	/// </summary>
	private async Task<bool> AddToListFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		LoadFromDbResult result = _entityLoader.LoadFromDb(filePath);

		RegenerateId(result.Folders, result.Files);

		int index = hierarchy.Count;

		result
		   .Folders
		   .OfType<ExplorerModelBase>()
		   .Concat(result.Files)
		   .Where(x => x.ParentId is null)
		   .OrderBy(x => x.Index)
		   .ForEach(x =>
		   {
			   x.Index = index;

			   index++;
		   });

		if (result.Folders.Length > 0 && !await _dbAccess
			.AddFoldersAsync(result.Folders, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		if (result.Files.Length > 0 && !await _dbAccess
			.AddFilesAsync(result.Files, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		objects.AddRange(_entityLoader.Map(result.Folders, result.Files));

		return true;
	}

	/// <summary>
	/// Exports data to JSON.
	/// </summary>
	private async Task ExportToJsonAsync(string filePath, CancellationToken token = default)
	{
		FolderModel[] dbFolders = await _dbAccess
			.GetAllFoldersAsync(token: token)
			.ConfigureAwait(false);

		FileModel[] dbFiles = await _dbAccess
			.GetAllFilesAsync(token: token)
			.ConfigureAwait(false);

		ExplorerModelBase[] entities = [.. dbFolders.Concat<ExplorerModelBase>(dbFiles)];

		_fileSystem.WriteAllText(
			filePath,
			_jsonSerializer.Serialize(entities, AppUtils.JsonOptions));
	}

	/// <summary>
	/// Exports data to SQLite database.
	/// </summary>
	private void ExportToSQLite(string filePath)
	{
		BackupSqliteParameters parameters = new()
		{
			ClearDestPool = true,
			ClearSourcePool = false,
			DestFilePath = filePath,
			SourceFilePath = _dbAccess.GetDbFilePath()
		};

		_dbAccess.BackupSqliteDatabase(parameters);
	}

	/// <summary>
	/// Imports data from JSON.
	/// </summary>
	private Task<bool> ImportFromJsonAsync(
		string filePath,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		return variant switch
		{
			ImportListVariant.Replace => ReplaceFromJsonAsync(
				filePath,
				objects,
				hierarchy,
				token),
			ImportListVariant.AddToList => AddToListFromJsonAsync(
				filePath,
				objects,
				hierarchy,
				token),
			_ => throw new NotImplementedException()
		};
	}

	/// <summary>
	/// Imports data from SQLite database.
	/// </summary>
	private Task<bool> ImportFromSQLiteAsync(
		string filePath,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		return variant switch
		{
			ImportListVariant.Replace => ReplaceFromSQLiteAsync(
				filePath,
				objects,
				hierarchy,
				token),
			ImportListVariant.AddToList => AddToListFromSQLiteAsync(
				filePath,
				objects,
				hierarchy,
				token),
			_ => throw new NotImplementedException()
		};
	}

	/// <summary>
	/// Replaces the list with data from json.
	/// </summary>
	private async Task<bool> ReplaceFromJsonAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		string json = _fileSystem.ReadAllText(filePath);

		if (_jsonSerializer.Deserialize<ExplorerModelBase[]>(json) is not ExplorerModelBase[] entities)
		{
			return false;
		}

		if (!_dbAccess.ClearDatabase())
		{
			return false;
		}

		FolderModel[] folders = [.. entities.OfType<FolderModel>()];

		FileModel[] files = [.. entities.OfType<FileModel>()];

		RegenerateId(folders, files);

		if (!await _dbAccess
			.AddFoldersAsync(folders, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		if (!await _dbAccess
			.AddFilesAsync(files, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		objects.AddRange(_entityLoader.Map(
			folders,
			files));

		hierarchy.Clear();

		return true;
	}

	/// <summary>
	/// Replaces the list with data from SQLite database.
	/// </summary>
	private async Task<bool> ReplaceFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		if (!await _dbAccess
			.RestoreFromBackupAsync(filePath, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		ExplorerModelBaseDto[] result = await _entityLoader
			.LoadFromEmbeddedDbAsync(token)
			.ConfigureAwait(false);

		objects.AddRange(result);

		hierarchy.Clear();

		return true;
	}
	#endregion
}
