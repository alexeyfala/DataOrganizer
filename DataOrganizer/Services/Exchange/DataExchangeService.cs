using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Dto.Dialogs;
using DataOrganizer.Dto.Exchange;
using DataOrganizer.Enums.Exchange;
using DataOrganizer.Extensions.Editor;
using DataOrganizer.Extensions.Hotkeys;
using DataOrganizer.Helpers.Editor;
using DataOrganizer.Helpers.Exchange;
using DataOrganizer.Interfaces.Dialogs;
using DataOrganizer.Interfaces.Exchange;
using DataOrganizer.Interfaces.Hierarchy;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Storage;
using Hierarchy.Dto;
using Notifications.Interfaces;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Exchange;

/// <inheritdoc cref="IDataExchangeService" />
public sealed class DataExchangeService : IDataExchangeService
{
	#region Data
	/// <inheritdoc cref="IBackedUpWriter" />
	private readonly IBackedUpWriter _backedUpWriter;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IDbBackupService" />
	private readonly IDbBackupService _dbBackupService;

	/// <inheritdoc cref="IDbImporter" />
	private readonly IDbImporter _dbImporter;

	/// <inheritdoc cref="IEntityLoader" />
	private readonly IEntityLoader _entityLoader;

	/// <summary>
	/// The exchange formats, in the order the file picker lists them.
	/// </summary>
	private readonly IExchangeFormat[] _formats;

	/// <inheritdoc cref="IImportModePrompt" />
	private readonly IImportModePrompt _importModePrompt;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;

	/// <inheritdoc cref="IFileSystemPicker" />
	private readonly IFileSystemPicker _picker;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <inheritdoc cref="IUnreadableHotkeysFinder" />
	private readonly IUnreadableHotkeysFinder _unreadableHotkeys;
	#endregion

	#region Constructors
	public DataExchangeService(
		IBackedUpWriter backedUpWriter,
		IDbAccess dbAccess,
		IDbBackupService dbBackupService,
		IDbImporter dbImporter,
		IEntityLoader entityLoader,
		IEnumerable<IExchangeFormat> formats,
		IFileSystemPicker picker,
		IImportModePrompt importModePrompt,
		ILogger logger,
		IMessenger messenger,
		INotificationService notification,
		IUnreadableHotkeysFinder unreadableHotkeys,
		TimeProvider timeProvider)
	{
		_backedUpWriter = backedUpWriter;

		_dbAccess = dbAccess;

		_dbBackupService = dbBackupService;

		_dbImporter = dbImporter;

		_entityLoader = entityLoader;

		_formats = [.. formats];

		_importModePrompt = importModePrompt;

		_logger = logger;

		_messenger = messenger;

		_notification = notification;

		_picker = picker;

		_timeProvider = timeProvider;

		_unreadableHotkeys = unreadableHotkeys;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ExportDataAsync(CancellationToken token = default)
	{
		FileSaveParameters parameters = new()
		{
			DefaultExtension = KnownFileExtensions.Json.TrimStart('.'),
			FileTypes = [.. _formats.Select(CreateFileType)],
			ShowOverwritePrompt = true,
			SuggestedFileName = AppInfo.AppName,
			Title = Strings.SaveAs
		};

		if (await _picker
			.SaveFileAsync(parameters)
			.ConfigureAwait(false) is not { } filePath)
		{
			return;
		}

		try
		{
			using ProgressScope _ = _messenger.ShowProgress();

			await GetFormat(filePath)
				.ExportAsync(filePath, token)
				.ConfigureAwait(false);

			_notification.ShowInformationSnackbar(Strings.DataExportCompleted);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

			_notification.ShowErrorSnackbar(Strings.FailedToExportData);
		}
	}

	/// <inheritdoc />
	public async Task<ImportDataResult?> ImportDataAsync(IReadOnlyCollection<ExplorerItemDtoBase> hierarchy)
	{
		ImportMode variant = ImportMode.Replace;

		if (hierarchy.Count != 0)
		{
			variant = await _importModePrompt
				.SelectImportModeAsync()
				.ConfigureAwait(true);

			if (variant == ImportMode.None)
			{
				return null;
			}
		}

		FileOpenParameters parameters = new()
		{
			AllowMultiple = false,
			FileTypes = [CreateAllFormatsFileType(), .. _formats.Select(CreateFileType)],
			Title = Strings.Select
		};

		string[] filePaths = await _picker
			.SelectFilesAsync(parameters)
			.ConfigureAwait(false);

		if (filePaths.IsEmpty())
		{
			return null;
		}

		ImportDataResult? result = null;

		try
		{
			// The database goes back to its copy when the import fails at any step.
			if (!await _backedUpWriter
				.WriteAsync(ImportAsync)
				.ConfigureAwait(false))
			{
				return null;
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar(Strings.FailedToImportData);

			return null;
		}

		return result;

		// Writes the objects of the file into the database; false when nothing could be read from it.
		async Task<bool> ImportAsync()
		{
			using ProgressScope _ = _messenger.ShowProgress();

			string filePath = filePaths[0];

			IExchangeFormat format = GetFormat(filePath);

			LoadedHierarchy? imported = variant == ImportMode.Replace && format.IsDatabaseCopy
				? await ReplaceDatabaseAsync(filePath).ConfigureAwait(false)
				: await AddObjectsAsync(format, filePath, variant, hierarchy).ConfigureAwait(false);

			if (imported is null)
			{
				_notification.ShowErrorSnackbar(Strings.FailedToImportData);

				return false;
			}

			FileDto[] unreadable = _unreadableHotkeys.Find(imported.Items);

			if (unreadable.IsNotEmpty())
			{
				await DropUnreadableHotkeysAsync(unreadable).ConfigureAwait(false);

				_notification.ShowErrorSnackbar(
					unreadable.GetUnreadableHotkeysPresentation(Strings.UnreadableHotkeysRemoved));
			}

			result = new(imported, variant);

			return true;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Describes the files of <paramref name="format" /> for the file picker.
	/// </summary>
	private static FileTypeFilter CreateFileType(IExchangeFormat format)
	{
		return new()
		{
			MimeTypes = [format.MimeType],
			Name = format.FileTypeName,
			Patterns = [$"*{format.Extension}"]
		};
	}

	/// <summary>
	/// Adds the folders and files of the file to the database, cleared first for a replacing import; <c>null</c> when the
	/// file holds none that can be read.
	/// </summary>
	private async Task<LoadedHierarchy?> AddObjectsAsync(
		IExchangeFormat format,
		string filePath,
		ImportMode variant,
		IReadOnlyCollection<ExplorerItemDtoBase> hierarchy)
	{
		if (await format
			.ReadAsync(filePath)
			.ConfigureAwait(false) is not { } entities)
		{
			return null;
		}

		if (variant == ImportMode.Replace)
		{
			await _dbImporter
				.ClearDatabaseAsync()
				.ConfigureAwait(false);
		}

		if (!format.IsDatabaseCopy)
		{
			// The file keeps no dates, so the objects get the time of the import.
			ImportedEntityRules.SetDates(
				entities.Folders,
				entities.Files,
				_timeProvider
					.GetLocalNow()
					.LocalDateTime);
		}

		ImportedEntityRules.RegenerateIds(entities.Folders, entities.Files);

		if (variant == ImportMode.Append)
		{
			ImportedEntityRules.PlaceAfter(hierarchy, entities.Folders, entities.Files);
		}

		if (entities.Folders.IsNotEmpty())
		{
			await _dbAccess
				.AddFoldersAsync(entities.Folders)
				.ConfigureAwait(false);
		}

		if (entities.Files.IsNotEmpty())
		{
			await _dbAccess
				.AddFilesAsync(entities.Files)
				.ConfigureAwait(false);
		}

		return _entityLoader.Map(entities.Folders, entities.Files);
	}

	/// <summary>
	/// Describes the files of every format at once for the file picker.
	/// </summary>
	private FileTypeFilter CreateAllFormatsFileType()
	{
		return new()
		{
			MimeTypes = [.. _formats.Select(static x => x.MimeType)],
			Name = "All Supported Files",
			Patterns = [.. _formats.Select(static x => $"*{x.Extension}")]
		};
	}

	/// <summary>
	/// Removes the hotkeys of the files whose sequence could not be read.
	/// </summary>
	private async Task DropUnreadableHotkeysAsync(FileDto[] files)
	{
		foreach (FileDto file in files)
		{
			if (!await _dbAccess
				.DeleteHotkeysAsync(file.Id)
				.ConfigureAwait(false))
			{
				_logger.LogError(
					$@"Hotkeys of file ""{file.Name}"" ({file.Id}) could not be removed.",
					breakInDebugger: false);

				continue;
			}

			file
				.Hotkeys
				.Clear();
		}
	}

	/// <summary>
	/// Returns the format of <paramref name="filePath" /> by its extension.
	/// </summary>
	private IExchangeFormat GetFormat(string filePath)
	{
		string extension = Path.GetExtension(filePath);

		return _formats.FirstOrDefault(x => x.Extension == extension)
			?? throw new NotSupportedException($@"No exchange format reads ""{extension}"" files.");
	}

	/// <summary>
	/// Puts a copy of the database in its place and reads the objects back; <c>null</c> when the copy is not a database
	/// or cannot be read.
	/// </summary>
	private async Task<LoadedHierarchy?> ReplaceDatabaseAsync(string filePath)
	{
		if (!_dbImporter.IsValidSqliteDatabase(filePath))
		{
			return null;
		}

		await _dbBackupService
			.RestoreFromBackupAsync(filePath)
			.ConfigureAwait(false);

		// A copy in place that cannot be read gives null, so the caller restores the copy it took.
		return await _entityLoader
			.LoadHierarchyAsync()
			.ConfigureAwait(false);
	}
	#endregion
}
