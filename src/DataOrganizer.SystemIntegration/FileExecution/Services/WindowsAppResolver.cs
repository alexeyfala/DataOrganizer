using DataOrganizer.Shared.Extensions;
using DataOrganizer.SystemIntegration.FileExecution.Dto;
using DataOrganizer.SystemIntegration.FileExecution.Interfaces;
using Serilog;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.FileExecution.Services;

/// <summary>
/// Opens a file in its associated program and asks the user for one when the association is missing or only leads to
/// the "Open with" dialog of Windows.
/// </summary>
public sealed class WindowsAppResolver : IAppResolver
{
	#region Data
	/// <inheritdoc cref="IAppPickerService" />
	private readonly IAppPickerService _appPicker;

	/// <inheritdoc cref="IFileAssociationService" />
	private readonly IFileAssociationService _fileAssociation;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="WindowsAppResolver" /> class.
	/// </summary>
	public WindowsAppResolver(
		IAppPickerService appPicker,
		IFileAssociationService fileAssociation,
		ILogger logger)
	{
		_appPicker = appPicker;

		_fileAssociation = fileAssociation;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<AppResolution> ResolveAsync(string filePath, CancellationToken token = default)
	{
		string fileName = Path.GetFileName(filePath);

		if (_fileAssociation.HasAssociatedApp(Path.GetExtension(fileName)))
		{
			_logger.LogDebug($@"File ""{fileName}"" opens in the program the operating system assigns");

			return new SystemAppResolution();
		}

		AssociatedAppInfo? selected = await _appPicker
			.PickAppAsync(filePath, token)
			.ConfigureAwait(false);

		if (selected is null)
		{
			_logger.LogInformation($@"User cancelled the application picker for ""{filePath}"".");

			return new CancelledAppResolution();
		}

		return new ChosenAppResolution(selected.AppPath);
	}
	#endregion
}
