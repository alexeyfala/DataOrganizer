using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using DataOrganizer.Windows;
using Entities.Models;
using Repository.DTO;
using Repository.Enums;
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

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="IFileSystemPicker" />
	private readonly IFileSystemPicker _picker;

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
		IMessenger messenger,
		IXmlSerializerWrapper xmlSerializer)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_messenger = messenger;

		_picker = picker;

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
			ShowProgressBar();

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemPicker.JsonExt:
					await ExportToJsonAsync(filePath, token).ConfigureAwait(false);
					break;

				case IFileSystemPicker.XmlExt:
					await ExportToXmlAsync(filePath, token).ConfigureAwait(false);
					break;

				case AppUtils.SQLiteExtension:
					await ExportToSQLiteAsync(filePath, token).ConfigureAwait(false);
					break;

				default:
					throw new NotImplementedException();
			}

			SendMessage(Strings.DataExportCompleted, SnackbarMessageLevel.Information);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			SendMessage(Strings.FailedToExportData, SnackbarMessageLevel.Error);
		}
		finally
		{
			HideProgressBar();
		}
	}

	/// <inheritdoc />
	public async Task<ImportDataResult?> ImportDataAsync(
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
				return null;
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
			return null;
		}

		if (await _dbAccess
			.BackupDatabaseAsync(token)
			.ConfigureAwait(false) is not { } backupFilePath || string.IsNullOrEmpty(backupFilePath))
		{
			SendMessage(Strings.UnableToCreateDatabaseBackup, SnackbarMessageLevel.Error);

			return null;
		}

		try
		{
			ShowProgressBar();

			string filePath = filePaths[0];

			List<ExplorerModelBaseDto> objects = [];

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemPicker.JsonExt:
					if (!await ImportFromJsonAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token).ConfigureAwait(false))
					{
						SendMessage(Strings.FailedToImportData, SnackbarMessageLevel.Error);

						await _dbAccess
							.RestoreFromBackupAsync(backupFilePath, token)
							.ConfigureAwait(false);

						return null;
					}
					break;

				case IFileSystemPicker.XmlExt:
					if (!await ImportFromXmlAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token).ConfigureAwait(false))
					{
						SendMessage(Strings.FailedToImportData, SnackbarMessageLevel.Error);

						await _dbAccess
							.RestoreFromBackupAsync(backupFilePath, token)
							.ConfigureAwait(false);

						return null;
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
						SendMessage(Strings.FailedToImportData, SnackbarMessageLevel.Error);

						await _dbAccess
							.RestoreFromBackupAsync(backupFilePath, token)
							.ConfigureAwait(false);

						return null;
					}
					break;

				default:
					throw new NotImplementedException();
			}

			return new()
			{
				ImportedItems = objects,
				Variant = variant
			};
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			SendMessage(Strings.FailedToImportData, SnackbarMessageLevel.Error);

			await _dbAccess
				.RestoreFromBackupAsync(backupFilePath, token)
				.ConfigureAwait(false);

			return null;
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

			HideProgressBar();
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
		if (variant == ImportListVariant.Replace && !await _dbAccess
			.ClearDatabaseAsync(token)
			.ConfigureAwait(false))
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

	#region Helpers
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

		ILookup<Guid?, FolderModel> foldersByParent = folders.ToLookup(x => x.ParentId);

		ILookup<Guid?, FileModel> filesByParent = files.ToLookup(x => x.ParentId);

		folders.ForEach(folder =>
		{
			Guid newFolderId = Guid.NewGuid();

			Guid oldFolderId = folder.Id;

			folder.Id = newFolderId;

			foldersByParent[oldFolderId].ForEach(x => x.ParentId = newFolderId);

			filesByParent[oldFolderId].ForEach(x => x.ParentId = newFolderId);
		});
	}

	/// <summary>
	/// Exports data to JSON.
	/// </summary>
	private async Task ExportToJsonAsync(string filePath, CancellationToken token)
	{
		ExplorerModelBase[] entities = await GetEntitiesFromDbAsync(token).ConfigureAwait(false);

		// Streaming serialization: writes Json directly to the file without
		// materializing the whole document as a string in memory.
		await using Stream stream = _fileSystem.CreateSequentialWrite(filePath);

		await _jsonSerializer
			.SerializeAsync(stream, entities, AppUtils.JsonOptions, token)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Exports data to SQLite database.
	/// </summary>
	private Task ExportToSQLiteAsync(string filePath, CancellationToken token)
	{
		BackupSqliteParameters parameters = new()
		{
			ClearDestPool = true,
			ClearSourcePool = false,
			DestFilePath = filePath,
			SourceFilePath = _dbAccess.GetDbFilePath()
		};

		return _dbAccess.BackupSqliteDatabaseAsync(parameters, token);
	}

	/// <summary>
	/// Exports data to XML.
	/// </summary>
	private async Task ExportToXmlAsync(string filePath, CancellationToken token)
	{
		ExplorerModelBase[] entities = await GetEntitiesFromDbAsync(token).ConfigureAwait(false);

		// Streaming serialization: XmlSerializer writes directly to the file
		// without materializing the whole document as a string in memory.
		await using Stream stream = _fileSystem.CreateSequentialWrite(filePath);

		_xmlSerializer.Serialize(stream, entities);
	}

	/// <summary>
	/// Load all entities from database.
	/// </summary>
	private async Task<ExplorerModelBase[]> GetEntitiesFromDbAsync(CancellationToken token)
	{
		FolderModel[] dbFolders = await _dbAccess
			.GetAllFoldersAsync(token)
			.ConfigureAwait(false);

		FileModel[] dbFiles = await _dbAccess
			.GetAllFilesAsync(OptionalFileProperty.Contents | OptionalFileProperty.Properties, token)
			.ConfigureAwait(false);

		return [.. dbFolders.Concat<ExplorerModelBase>(dbFiles)];
	}

	/// <summary>
	/// Sends <see cref="ShowProgressBarMessage" /> to hide progress bar in the editor.
	/// </summary>
	private void HideProgressBar() => _messenger.Send(new ShowProgressBarMessage(false));

	/// <summary>
	/// Imports data from JSON.
	/// </summary>
	private async Task<bool> ImportFromJsonAsync(
		string filePath,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		// Streaming deserialization: avoids loading the entire file into a string before parsing.
		ExplorerModelBase[]? entities;

		await using (Stream stream = _fileSystem.OpenSequentialRead(filePath))
		{
			entities = await _jsonSerializer
				.DeserializeAsync<ExplorerModelBase[]>(stream, token)
				.ConfigureAwait(false);
		}

		if (entities is null)
		{
			return false;
		}

		return await ImportEntitiesAsync(
			entities,
			variant,
			objects,
			hierarchy,
			token).ConfigureAwait(false);
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
	private async Task<bool> ImportFromXmlAsync(
		string filePath,
		ImportListVariant variant,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token)
	{
		// Streaming deserialization: XmlSerializer reads directly from the file
		// without materializing the whole document as a string in memory.
		ExplorerModelBase[]? entities;

		await using (Stream stream = _fileSystem.OpenSequentialRead(filePath))
		{
			entities = _xmlSerializer.Deserialize<ExplorerModelBase[]>(stream);
		}

		if (entities is null)
		{
			return false;
		}

		return await ImportEntitiesAsync(
			entities,
			variant,
			objects,
			hierarchy,
			token).ConfigureAwait(false);
	}

	/// <summary>
	/// Sends <see cref="ShowSnackbarMessage" /> to recepient.
	/// </summary>
	private void SendMessage(string message, SnackbarMessageLevel level)
	{
		_messenger.Send(new ShowSnackbarMessage(message, level));
	}

	/// <summary>
	/// Sends <see cref="ShowProgressBarMessage" /> to display progress bar in the editor.
	/// </summary>
	private void ShowProgressBar() => _messenger.Send(new ShowProgressBarMessage(true));
	#endregion
}
