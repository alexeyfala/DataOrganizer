using Avalonia.Platform.Storage;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using DialogHostAvalonia;
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

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

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
		IEntityLoader entityLoader,
		IFileSystem fileSystem,
		IFileSystemEnrtyPicker picker,
		ILogger logger,
		IViewFactory viewFactory,
		IViewModelExecutionService viewModel)
	{
		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

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
					break;

				case IFileSystemEnrtyPicker.XmlExt:
					break;

				case AppUtils.SQLiteExtension:
					BackupSqliteParameters parameters = new()
					{
						ClearDestPool = true,
						ClearSourcePool = false,
						DestFilePath = filePath,
						SourceFilePath = _dbAccess.GetDbFilePath()
					};

					_dbAccess.BackupSqliteDatabase(parameters);
					break;

				default:
					throw new NotImplementedException();
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
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
			ImportListSelectorView view = _viewFactory.CreateUserControl<ImportListSelectorView>();

			view
				.ViewModel
				.Header = Strings.ImportingObjects;

			_ = DialogHost.Show(view);

			ImportListVariant result = await view
				.ViewModel
				.GetResultAsync(token)
				.ConfigureAwait(false);

			if (result == ImportListVariant.None)
			{
				return;
			}

			variant = result;
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
			FileModel[] childFiles = [.. files.Where(x => folder.Id == x.ParentId)];

			FolderModel[] childFolders = [.. folders.Where(x => folder.Id == x.ParentId)];

			Guid newFolderId = Guid.NewGuid();

			folder.Id = newFolderId;

			childFiles.ForEach(x => x.ParentId = newFolderId);

			childFolders.ForEach(x => x.ParentId = newFolderId);
		});
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

		hierarchy.Clear();

		ExplorerModelBaseDto[] result = await _entityLoader
			.LoadFromEmbeddedDbAsync(token)
			.ConfigureAwait(false);

		objects.AddRange(result);

		return true;
	}
	#endregion
}
