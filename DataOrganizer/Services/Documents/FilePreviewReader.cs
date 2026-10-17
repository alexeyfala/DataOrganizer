using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Documents;
using DataOrganizer.Interfaces.Notifications;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Documents;

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

		switch (reading.Status)
		{
			case FileTextStatus.Damaged:
				_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");

				return null;

			case FileTextStatus.NotText:
				// A preview is rendered on demand, so contents that are not text only reach the log.
				_logger.LogDebug($@"{Strings.NonTextFileContents} of file ""{file.Id}""");

				return null;

			case FileTextStatus.Empty:
				_notification.ShowInformationSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

				return null;

			case FileTextStatus.Closed:
				return null;
		}

		_logger.LogDebug($@"Display content prewiew for ""{file.Id}""");

		return reading.Text.Truncate(MaxPreviewLength);
	}
	#endregion
}
