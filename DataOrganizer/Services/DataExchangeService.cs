using Avalonia.Platform.Storage;
using DataOrganizer.Interfaces;
using DataOrganizer.Windows;
using Repository.DTO;
using Repository.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using Shared.Properties;
using System;
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

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IFileSystemEnrtyPicker" />
	private readonly IFileSystemEnrtyPicker _picker;

	/// <inheritdoc cref="IViewModelExecutionService" />
	private readonly IViewModelExecutionService _viewModel;
	#endregion

	#region Constructors
	public DataExchangeService(
		IDbAccess dbAccess,
		IEntityLoader entityLoader,
		IFileSystemEnrtyPicker picker,
		ILogger logger,
		IViewModelExecutionService viewModel)
	{
		_dbAccess = dbAccess;

		_entityLoader = entityLoader;

		_logger = logger;

		_picker = picker;

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
	#endregion
}
