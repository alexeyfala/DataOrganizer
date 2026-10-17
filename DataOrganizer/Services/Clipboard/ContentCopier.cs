using DataOrganizer.Dto.Entities;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Notifications;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

/// <inheritdoc cref="IContentCopier" />
public sealed class ContentCopier : IContentCopier
{
	#region Data
	/// <inheritdoc cref="ICopyHistory" />
	private readonly ICopyHistory _copyHistory;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="IFileTextCopier" />
	private readonly IFileTextCopier _fileTextCopier;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public ContentCopier(
		ICopyHistory copyHistory,
		IDbAccess dbAccess,
		IFileTextCopier fileTextCopier,
		ILogger logger,
		INotificationService notification)
	{
		_copyHistory = copyHistory;

		_dbAccess = dbAccess;

		_fileTextCopier = fileTextCopier;

		_logger = logger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task<bool> CopyAsync(
		FileDto file,
		bool showAtOnce,
		CancellationToken token = default)
	{
		try
		{
			if (!await _dbAccess
				.ExistsAsync(file.Id, token)
				.ConfigureAwait(true))
			{
				_notification.ShowErrorSnackbar($@"""{file.Name}"" {Strings.DoesNotExist}");

				return false;
			}

			switch (await _fileTextCopier
				.CopyAsync(file, Strings.CopyContent, token)
				.ConfigureAwait(true))
			{
				case FileTextStatus.Damaged:
					_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");

					return false;

				case FileTextStatus.NotText:
					_notification.ShowWarningSnackbar($@"{Strings.NonTextFileContents}: ""{file.Name}""");

					return false;

				case FileTextStatus.Empty:
					_notification.ShowInformationSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");

					return false;

				case FileTextStatus.Closed:
					return false;
			}

			_copyHistory.Add(file, showAtOnce);

			return true;
		}
		catch (OperationCanceledException)
		{
			// The copy was given up on, which is normal flow.
			return false;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");

			return false;
		}
	}
	#endregion
}
