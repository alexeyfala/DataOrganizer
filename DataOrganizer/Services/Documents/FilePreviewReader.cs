using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Documents;
using DataOrganizer.Interfaces.Documents;
using Hierarchy.Dto;
using Notifications.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

/// <inheritdoc cref="IFilePreviewReader" />
public sealed class FilePreviewReader : IFilePreviewReader
{
	#region Data
	/// <summary>
	/// Longest start of a text that a preview shows.
	/// </summary>
	private const int MaxPreviewLength = 200;

	/// <inheritdoc cref="IFileTextReader" />
	private readonly IFileTextReader _fileTextReader;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FilePreviewReader(
		IFileTextReader fileTextReader,
		ILogger logger,
		INotificationService notification)
	{
		_fileTextReader = fileTextReader;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<string?> ReadPreviewAsync(FileDto file)
	{
		FileTextReading reading;

		try
		{
			reading = await _fileTextReader
				.ReadUnlockedAsync(file)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			// A preview is rendered on demand, so the failure only reaches the log.
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}

		if (!FileTextStatusRules.HoldsText(
			reading.Status,
			() => _logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}"""),
			// A preview is rendered on demand, so contents that are not text only reach the log.
			() => _logger.LogDebug($@"{Strings.NonTextFileContents} of file ""{file.Id}"""),
			() => _notification.ShowInformationSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""")))
		{
			return null;
		}

		_logger.LogDebug($@"Display content prewiew for ""{file.Id}""");

		return reading.Text.Truncate(MaxPreviewLength);
	}
	#endregion
}
