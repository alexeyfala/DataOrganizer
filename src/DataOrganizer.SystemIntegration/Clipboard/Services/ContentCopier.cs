using DataOrganizer.Database.Repository.Interfaces.Database;
using DataOrganizer.Documents.FileContents.Helpers;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Infrastructure.DialogContracts.Helpers;
using DataOrganizer.Infrastructure.Notifications.Interfaces;
using DataOrganizer.Shared.Extensions;
using DataOrganizer.Shared.Helpers;
using DataOrganizer.Shared.Properties;
using DataOrganizer.SystemIntegration.Clipboard.Interfaces;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.SystemIntegration.Clipboard.Services;

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
	/// <summary>
	/// Initializes a new instance of the <see cref="ContentCopier" /> class.
	/// </summary>
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
				_notification.ShowErrorSnackbar($"{QuoteBuilder.Build(file.Name)} {Strings.DoesNotExist}");

				return false;
			}

			if (!await CopyTextAsync(
				file,
				ObjectHeaderBuilder.Build(Strings.CopyContent, file.Name),
				token).ConfigureAwait(true))
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

			_notification.ShowErrorSnackbar(FileContentsMessages.FailedToLoad(file.Name));

			return false;
		}
	}

	/// <inheritdoc />
	protected override void ReportDamaged(FileDto file)
	{
		_notification.ShowErrorSnackbar(FileContentsMessages.FailedToLoad(file.Name));
	}

	/// <inheritdoc />
	protected override void ReportEmpty(FileDto file)
	{
		_notification.ShowInformationSnackbar(FileContentsMessages.Empty(file.Name));
	}

	/// <inheritdoc />
	protected override void ReportNotText(FileDto file)
	{
		_notification.ShowWarningSnackbar(FileContentsMessages.NotText(file.Name));
	}
	#endregion
}
