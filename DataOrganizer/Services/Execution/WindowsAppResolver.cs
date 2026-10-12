using DataOrganizer.Dto.Execution;
using DataOrganizer.Interfaces.Execution;
using Serilog;
using Shared.Extensions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Execution;

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

		string? appPath = _fileAssociation.FindApplicationByExtension(Path.GetExtension(fileName));

		if (appPath?.EndsWith("OpenWith.exe", StringComparison.OrdinalIgnoreCase) == false)
		{
			_logger.LogDebug($@"Application path to open file ""{fileName}"" is: {appPath}");

			return new(IsCancelled: false, AppPath: null);
		}

		AssociatedAppInfo? selected = await _appPicker
			.PickAppAsync(filePath, token)
			.ConfigureAwait(false);

		if (selected is null)
		{
			_logger.LogInformation($@"User cancelled the application picker for ""{filePath}"".");

			return new(IsCancelled: true, AppPath: null);
		}

		return new(IsCancelled: false, AppPath: selected.AppPath);
	}
	#endregion
}
