using Avalonia.Platform.Storage;
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

	/// <inheritdoc cref="IFileSystemPicker" />
	private readonly IFileSystemPicker _picker;

	/// <inheritdoc cref="IViewModelExecutionService" />
	private readonly IViewModelExecutionService _viewModel;

	/// <inheritdoc cref="IXmlSerializerWrapper" />
	private readonly IXmlSerializerWrapper _xmlSerializer;
	#endregion

	#region Constructors
	public DataExchangeService(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityLoader entityLoader,
		IFileSystem fileSystem,
		IFileSystemPicker picker,
		IJsonSerializerWrapper jsonSerializer,
		ILogger logger,
		IViewModelExecutionService viewModel,
		IXmlSerializerWrapper xmlSerializer)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_picker = picker;

		_viewModel = viewModel;

		_xmlSerializer = xmlSerializer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> AppendFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		LoadFromDbResult result = _dbAccess.LoadFromDb(filePath);

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

		if (result.Folders.IsNotEmpty() && !await _dbAccess
			.AddFoldersAsync(result.Folders, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		if (result.Files.IsNotEmpty() && !await _dbAccess
			.AddFilesAsync(result.Files, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		objects.AddRange(_entityLoader.Map(result.Folders, result.Files));

		return true;
	}

	/// <inheritdoc />
	public async Task ExportDataAsync(CancellationToken token = default)
	{
		FilePickerSaveOptions options = new()
		{
			DefaultExtension = IFileSystemPicker.JsonExt.TrimStart('.'),
			FileTypeChoices = IFileSystemPicker.ImportExportFilePickerTypes,
			ShowOverwritePrompt = true,
			SuggestedFileName = AppUtils.AppNameAsOneWord,
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
				case IFileSystemPicker.JsonExt:
					await ExportToJsonAsync(filePath, token).ConfigureAwait(false);
					break;

				case IFileSystemPicker.XmlExt:
					await ExportToXmlAsync(filePath, token).ConfigureAwait(false);
					break;

				case AppUtils.SQLiteExtension:
					ExportToSQLite(filePath);
					break;

				default:
					throw new NotImplementedException();
			}

			_viewModel.ExecuteInEditor(x => x.ShowInfoSnackbar(Strings.DataExportCompleted));
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
	public async Task<bool> ImportDataAsync(
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		ImportListVariant variant = ImportListVariant.Replace;

		if (hierarchy.Count != 0)
		{
			variant = await _dialogService
				.SelectImportVariantAsync(token)
				.ConfigureAwait(true);

			if (variant == ImportListVariant.None)
			{
				return false;
			}
		}

		FilePickerOpenOptions options = new()
		{
			AllowMultiple = false,
			FileTypeFilter = IFileSystemPicker.ImportExportFilePickerTypes,
			Title = Strings.Select
		};

		string[] filePaths = await _picker
			.SelectFilesAsync<EditorWindow>(options)
			.ConfigureAwait(false);

		if (filePaths.IsEmpty())
		{
			return false;
		}

		if (_dbAccess.BackupDatabase() is not { } backupFilePath || string.IsNullOrEmpty(backupFilePath))
		{
			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup));

			return false;
		}

		try
		{
			_viewModel.ExecuteInEditor(x => x.IsActionInProgress = true);

			// The path may contain %20 instead of spaces, so it needs to be decoded.
			string filePath = WebUtility.UrlDecode(filePaths[0]);

			List<ExplorerModelBaseDto> objects = [];

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemPicker.JsonExt:
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

						return false;
					}
					break;

				case IFileSystemPicker.XmlExt:
					if (!await ImportFromXmlAsync(
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

						return false;
					}
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

						return false;
					}
					break;

				default:
					throw new NotImplementedException();
			}

			_viewModel.ExecuteInEditor(viewModel =>
			{
				if (variant == ImportListVariant.Replace)
				{
					viewModel
						.CopyHistorySettings
						.Items
						.Clear();

					viewModel.IsRightSideSheetOpened = false;
				}

				viewModel.AddHierarchy(objects);

				viewModel.ShowInfoSnackbar(Strings.DataImportCompleted);
			});

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_viewModel.ExecuteInEditor(x => x.ShowErrorSnackbar(Strings.FailedToImportData));

			await _dbAccess
				.RestoreFromBackupAsync(backupFilePath, token)
				.ConfigureAwait(false);

			return false;
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

	/// <inheritdoc />
	public async Task<bool> ImportEntitiesAsync(
		ExplorerModelBase[] entities,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		if (variant == ImportListVariant.Replace && !_dbAccess.ClearDatabase())
		{
			return false;
		}

		DateTime now = DateTime.Now;

		entities.ForEach(x => x.CreatedDate = x.UpdatedDate = now);

		FolderModel[] folders = [.. entities.OfType<FolderModel>()];

		FileModel[] files = [.. entities.OfType<FileModel>()];

		RegenerateId(folders, files);

		if (folders.IsNotEmpty() && !await _dbAccess
			.AddFoldersAsync(folders, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		if (files.IsNotEmpty() && !await _dbAccess
			.AddFilesAsync(files, token)
			.ConfigureAwait(false))
		{
			return false;
		}

		objects.AddRange(_entityLoader.Map(
			folders,
			files));

		if (variant == ImportListVariant.Replace)
		{
			hierarchy.Clear();
		}

		return true;
	}

	/// <inheritdoc />
	public async Task<bool> ReplaceFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
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
	/// Exports data to JSON.
	/// </summary>
	private async Task ExportToJsonAsync(string filePath, CancellationToken token)
	{
		ExplorerModelBase[] entities = await GetEntitiesFromDbAsync(token).ConfigureAwait(false);

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
	/// Exports data to XML.
	/// </summary>
	private async Task ExportToXmlAsync(string filePath, CancellationToken token)
	{
		ExplorerModelBase[] entities = await GetEntitiesFromDbAsync(token).ConfigureAwait(false);

		_fileSystem.WriteAllText(
			filePath,
			_xmlSerializer.Serialize(entities));
	}

	/// <summary>
	/// Load all entities from database.
	/// </summary>
	private async Task<ExplorerModelBase[]> GetEntitiesFromDbAsync(CancellationToken token)
	{
		FolderModel[] dbFolders = await _dbAccess
			.GetAllFoldersAsync(token: token)
			.ConfigureAwait(false);

		FileModel[] dbFiles = await _dbAccess
			.GetAllFilesAsync(token: token)
			.ConfigureAwait(false);

		return [.. dbFolders.Concat<ExplorerModelBase>(dbFiles)];
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
		string json = _fileSystem.ReadAllText(filePath);

		if (_jsonSerializer.Deserialize<ExplorerModelBase[]>(json) is not { } entities)
		{
			return Task.FromResult(false);
		}

		return ImportEntitiesAsync(
			entities,
			variant,
			objects,
			hierarchy,
			token);
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
			ImportListVariant.Append => AppendFromSQLiteAsync(
				filePath,
				objects,
				hierarchy,
				token),
			_ => throw new NotImplementedException()
		};
	}

	/// <summary>
	/// Imports data from XML.
	/// </summary>
	private Task<bool> ImportFromXmlAsync(
		string filePath,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		string xml = _fileSystem.ReadAllText(filePath);

		if (_xmlSerializer.Deserialize<ExplorerModelBase[]>(xml) is not { } entities)
		{
			return Task.FromResult(false);
		}

		return ImportEntitiesAsync(
			entities,
			variant,
			objects,
			hierarchy,
			token);
	}
	#endregion
}
