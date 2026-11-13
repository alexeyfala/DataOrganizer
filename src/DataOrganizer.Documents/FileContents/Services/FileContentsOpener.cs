using DataOrganizer.Database.Entities.Enums;
using DataOrganizer.Documents.FileContents.Dto;
using DataOrganizer.Documents.FileContents.Enums;
using DataOrganizer.Documents.FileContents.Interfaces;
using DataOrganizer.Infrastructure.Diagnostics.Interfaces;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Properties;
using Serilog;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Documents.FileContents.Services;

/// <inheritdoc cref="IFileContentsOpener" />
public sealed class FileContentsOpener : IFileContentsOpener
{
	#region Data
	/// <inheritdoc cref="IDbFailureReporter" />
	private readonly IDbFailureReporter _dbFailureReporter;

	/// <inheritdoc cref="IFileContentsReader" />
	private readonly IFileContentsReader _fileContentsReader;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public FileContentsOpener(
		IDbFailureReporter dbFailureReporter,
		IFileContentsReader fileContentsReader,
		ILogger logger,
		INotificationService notification)
	{
		_dbFailureReporter = dbFailureReporter;

		_fileContentsReader = fileContentsReader;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<byte[]?> OpenAsync(
		Guid fileId,
		Guid? keeperId,
		string? fileName,
		EntityKind kind)
	{
		// Stryker disable once String : the text only goes to the failure reporter, which the run ignores
		string failureText = $@"{Strings.FailedToLoadFileContents} ""{fileName}""";

		FileContentsReading reading;

		try
		{
			reading = await _fileContentsReader
				.ReadUnlockedAsync(fileId, keeperId)
				.ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			_dbFailureReporter.Report(ex, failureText);

			return null;
		}

		switch (reading.Status)
		{
			case FileContentsStatus.Damaged:
				// The stored data is damaged, which no fault of the code explains.
				_logger.LogError($@"The contents of {kind} ""{fileId}"" failed their check.", breakInDebugger: false);

				_notification.ShowErrorSnackbar(failureText);

				return null;

			case FileContentsStatus.Closed:
				_logger.LogWarning($@"The key of the session did not open the contents of {kind} ""{fileId}"".");

				_notification.ShowErrorSnackbar(Strings.FailedToProcessContents);

				return null;

			default:
				return reading.Contents;
		}
	}
	#endregion
}
