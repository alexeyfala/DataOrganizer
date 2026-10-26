using DataOrganizer.Interfaces.Clipboard;
using Hierarchy.Dto;
using Notifications.Interfaces;
using Repository.Interfaces.Database;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

/// <inheritdoc cref="IContentCopier" />
public sealed class ContentCopier : ContentCopierBase, IContentCopier
{
	#region Data
	/// <inheritdoc cref="ICopyHistoryRecorder" />
	private readonly ICopyHistoryRecorder _copyHistory;

	/// <inheritdoc cref="IDbAccess" />
	private readonly IDbAccess _dbAccess;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public ContentCopier(
		ICopyHistoryRecorder copyHistory,
		IDbAccess dbAccess,
		IFileTextCopier fileTextCopier,
		ILogger logger,
		INotificationService notification) : base(fileTextCopier)
	{
		_copyHistory = copyHistory;

		_dbAccess = dbAccess;

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

			if (!await CopyTextAsync(file, Strings.CopyContent, token).ConfigureAwait(true))
			{
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

	/// <inheritdoc />
	protected override void ReportDamaged(FileDto file)
	{
		_notification.ShowErrorSnackbar($@"{Strings.FailedToLoadFileContents} ""{file.Name}""");
	}

	/// <inheritdoc />
	protected override void ReportEmpty(FileDto file)
	{
		_notification.ShowInformationSnackbar($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");
	}

	/// <inheritdoc />
	protected override void ReportNotText(FileDto file)
	{
		_notification.ShowWarningSnackbar($@"{Strings.NonTextFileContents}: ""{file.Name}""");
	}
	#endregion

}
