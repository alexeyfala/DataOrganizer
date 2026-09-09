using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using Entities.Helpers;
using Entities.Models;
using Repository.Dto;
using Repository.Enums;
using Repository.Interfaces;
using Repository.Services;
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
using System.Xml.Linq;

namespace DataOrganizer.Services;

public sealed class DataExchangeService : IDataExchangeService
{
	#region Data
	/// <summary>
	/// MIME type for JSON files.
	/// </summary>
	private const string JsonMime = "application/json";

	/// <summary>
	/// MIME type for SQLite database files.
	/// </summary>
	private const string SqliteMime = "application/x-sqlite3";

	/// <summary>
	/// MIME type for XML files.
	/// </summary>
	private const string XmlMime = "application/xml";

	/// <summary>
	/// File types for export application objects.
	/// </summary>
	private static readonly FilePickerFileType[] ExportFilePickerTypes;

	/// <summary>
	/// File types for import application objects.
	/// </summary>
	private static readonly FilePickerFileType[] ImportFilePickerTypes;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDialogService" />
	private readonly IDialogService _dialogService;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <inheritdoc cref="IFileSystem" />
	private readonly IFileSystem _fileSystem;

	/// <inheritdoc cref="IJsonSerializer" />
	private readonly IJsonSerializer _jsonSerializer;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IFileSystemPicker" />
	private readonly IFileSystemPicker _picker;

	/// <inheritdoc cref="IXmlSerializer" />
	private readonly IXmlSerializer _xmlSerializer;
	#endregion

	#region Constructors
	static DataExchangeService()
	{
		FilePickerFileType[] allTypes =
		[
			new("All Supported Files")
			{
				Patterns = [$"*{KnownFileExtensions.Json}", $"*{KnownFileExtensions.Xml}", $"*{KnownFileExtensions.Sqlite}"],
				MimeTypes = [JsonMime, XmlMime, SqliteMime]
			}
		];

		FilePickerFileType[] concreteTypes =
		[
			new("JSON File")
			{
				Patterns = [$"*{KnownFileExtensions.Json}"],
				MimeTypes = [JsonMime]
			},
			new("XML File")
			{
				Patterns = [$"*{KnownFileExtensions.Xml}"],
				MimeTypes = [XmlMime]
			},
			new("SQLite Database File")
			{
				Patterns = [$"*{KnownFileExtensions.Sqlite}"],
				MimeTypes = [SqliteMime]
			}
		];

		ExportFilePickerTypes = concreteTypes;

		ImportFilePickerTypes = [.. allTypes, .. concreteTypes];
	}

	public DataExchangeService(
		IDbAccess dbAccess,
		IDialogService dialogService,
		IEntityLoader entityLoader,
		IFileSystem fileSystem,
		IFileSystemPicker picker,
		IJsonSerializer jsonSerializer,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		IXmlSerializer xmlSerializer)
	{
		_dbAccess = dbAccess;

		_dialogService = dialogService;

		_entityLoader = entityLoader;

		_fileSystem = fileSystem;

		_jsonSerializer = jsonSerializer;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_picker = picker;

		_xmlSerializer = xmlSerializer;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ExportDataAsync(CancellationToken token = default)
	{
		FilePickerSaveOptions options = new()
		{
			DefaultExtension = KnownFileExtensions.Json.TrimStart('.'),
			FileTypeChoices = ExportFilePickerTypes,
			ShowOverwritePrompt = true,
			SuggestedFileName = AppInfo.AppName,
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
			using ProgressScope _ = _messenger.ShowProgress();

			switch (Path.GetExtension(filePath))
			{
				case KnownFileExtensions.Json:
					await ExportToJsonAsync(filePath, token).ConfigureAwait(false);
					break;

				case KnownFileExtensions.Xml:
					await ExportToXmlAsync(filePath, token).ConfigureAwait(false);
					break;

				case KnownFileExtensions.Sqlite:
					await ExportToSQLiteAsync(filePath, token).ConfigureAwait(false);
					break;

				default:
					throw new NotImplementedException();
			}

			_notification.ShowInformationSnackbar(Strings.DataExportCompleted);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_notification.ShowErrorSnackbar(Strings.FailedToExportData);
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
			FileTypeFilter = ImportFilePickerTypes,
			Title = Strings.Select
		};

		string[] filePaths = await _picker
			.SelectFilesAsync<EditorWindow>(options)
			.ConfigureAwait(false);

		if (filePaths.IsEmpty())
		{
			return null;
		}

		using DatabaseBackup? backup = await _dbAccess
			.BackupDatabaseAsync(token)
			.ConfigureAwait(false);

		if (backup is null)
		{
			_notification.ShowErrorSnackbar(Strings.UnableToCreateDatabaseBackup);

			return null;
		}

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			string filePath = filePaths[0];

			List<ExplorerModelBaseDto> objects = [];

			switch (Path.GetExtension(filePath))
			{
				case KnownFileExtensions.Json:
					if (!await ImportFromJsonAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token).ConfigureAwait(false))
					{
						_notification.ShowErrorSnackbar(Strings.FailedToImportData);

						await _dbAccess
							.RestoreFromBackupAsync(backup.FilePath, token)
							.ConfigureAwait(false);

						return null;
					}
					break;

				case KnownFileExtensions.Xml:
					if (!await ImportFromXmlAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token).ConfigureAwait(false))
					{
						_notification.ShowErrorSnackbar(Strings.FailedToImportData);

						await _dbAccess
							.RestoreFromBackupAsync(backup.FilePath, token)
							.ConfigureAwait(false);

						return null;
					}
					break;

				case KnownFileExtensions.Sqlite:
					if (!_dbAccess.IsValidSQLiteDatabase(filePath) || !await ImportFromSQLiteAsync(
						filePath,
						variant,
						objects,
						hierarchy,
						token).ConfigureAwait(false))
					{
						_notification.ShowErrorSnackbar(Strings.FailedToImportData);

						await _dbAccess
							.RestoreFromBackupAsync(backup.FilePath, token)
							.ConfigureAwait(false);

						return null;
					}
					break;

				default:
					throw new NotImplementedException();
			}

			FileModelDto[] unreadable = [.. objects.GetFilesWithUnreadableHotkeys()];

			if (unreadable.IsNotEmpty())
			{
				unreadable.ForEach(x =>
				{
					_logger.LogError(
						$@"Hotkeys of file ""{x.Name}"" ({x.Id}) could not be read.",
						assertDebug: false);
				});

				await DropUnreadableHotkeysAsync(unreadable, token).ConfigureAwait(false);

				_notification.ShowErrorSnackbar(
					unreadable.GetUnreadableHotkeysPresentation(Strings.UnreadableHotkeysRemoved));
			}

			return new(objects, variant);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			_notification.ShowErrorSnackbar(Strings.FailedToImportData);

			await _dbAccess
				.RestoreFromBackupAsync(backup.FilePath, token)
				.ConfigureAwait(false);

			return null;
		}
	}

	/// <summary>
	/// Appends data from SQLite database.
	/// </summary>
	internal async Task<bool> AppendFromSQLiteAsync(
		string filePath,
		List<ExplorerModelBaseDto> objects,
		Collection<ExplorerModelBaseDto> hierarchy,
		CancellationToken token = default)
	{
		LoadedEntities result = _dbAccess.LoadFromDb(filePath);

		RegenerateId(result.Folders, result.Files);

		SetupIndex(hierarchy, result.Folders, result.Files);

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

	/// <summary>
	/// Imports entities.
	/// </summary>
	internal async Task<bool> ImportEntitiesAsync(
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

		if (variant == ImportListVariant.Append)
		{
			SetupIndex(hierarchy, folders, files);
		}

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

	/// <summary>
	/// Replaces with data from SQLite database.
	/// </summary>
	internal async Task<bool> ReplaceFromSQLiteAsync(
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

		if (await _entityLoader
			.LoadFromEmbeddedDbAsync(token)
			.ConfigureAwait(false) is not { } result)
		{
			// The imported database is in place but unreadable, so the caller restores the copy it took.
			return false;
		}

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
	/// Sets <see cref="EntityModelBase.Index" /> to <paramref name="folders"/> and <paramref name="files"/>
	/// from <paramref name="hierarchy"/> max element index.
	/// </summary>
	private static void SetupIndex(
		Collection<ExplorerModelBaseDto> hierarchy,
		FolderModel[] folders,
		FileModel[] files)
	{
		if (hierarchy.Count == 0)
		{
			return;
		}

		int startIndex = hierarchy.Max(x => x.Index) + 1;

		folders
		   .OfType<ExplorerModelBase>()
		   .Concat(files)
		   .Where(x => x.ParentId is null)
		   .OrderBy(x => x.Index)
		   .ForEach(x =>
		   {
			   x.Index = startIndex;

			   startIndex++;
		   });
	}

	/// <summary>
	/// Removes the hotkeys of the files whose sequence could not be read.
	/// </summary>
	private async Task DropUnreadableHotkeysAsync(FileModelDto[] files, CancellationToken token)
	{
		foreach (FileModelDto file in files)
		{
			if (!await _dbAccess
				.DeleteHotkeysAsync(file.Id, token)
				.ConfigureAwait(false))
			{
				_logger.LogError(
					$@"Hotkeys of file ""{file.Name}"" ({file.Id}) could not be removed.",
					assertDebug: false);

				continue;
			}

			file
				.Hotkeys
				.Clear();

			file.SetHotkeysToolTip();
		}
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
			.SerializeAsync(stream, entities, JsonDefaults.Options, token)
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
			.GetAllFilesAsync(OptionalFileProperties.Contents | OptionalFileProperties.Properties, token)
			.ConfigureAwait(false);

		return [.. dbFolders.Concat<ExplorerModelBase>(dbFiles)];
	}

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
		// The document is read as a whole, so that hotkey names the library no longer knows
		// can be replaced before the serializer rejects the whole file because of them.
		ExplorerModelBase[]? entities;

		await using (Stream stream = _fileSystem.OpenSequentialRead(filePath))
		{
			XDocument document = await _xmlSerializer
				.LoadDocumentAsync(stream, token)
				.ConfigureAwait(false);

			HotkeyXmlSanitizer.Sanitize(document);

			entities = _xmlSerializer.Deserialize<ExplorerModelBase[]>(document);
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
	#endregion
}
