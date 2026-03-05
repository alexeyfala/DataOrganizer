using Avalonia.Platform.Storage;
using DataOrganizer.DTO.Entities.Abstract;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Views;
using DataOrganizer.Windows;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Interfaces;
using Shared.Properties;
using System;
using System.Collections.ObjectModel;
using System.IO;
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

			string filePath = filePaths[0];

			switch (Path.GetExtension(filePath))
			{
				case IFileSystemEnrtyPicker.JsonExt:
					break;

				case IFileSystemEnrtyPicker.XmlExt:
					break;

				case AppUtils.SQLiteExtension:
					switch (variant)
					{
						case ImportListVariant.Replace:
							if (!await _dbAccess
								.RestoreFromBackupAsync(filePath, token)
								.ConfigureAwait(false))
							{
								return;
							}

							hierarchy.Clear();

							ExplorerModelBaseDto[] entities = await _entityLoader
								.LoadAllHierarchyFromDbAsync(token)
								.ConfigureAwait(false);

							_viewModel.ExecuteInEditor(x => x.AddHierarchy(entities));
							break;

						case ImportListVariant.AddToTheEnd:
							break;

						default:
							throw new NotImplementedException();
					}
					break;

				default:
					throw new NotImplementedException();
			}

			_viewModel.ExecuteInEditor(x => x
				.CopyHistorySettings
				.CopyHistory
				.Clear());
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);

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
}
