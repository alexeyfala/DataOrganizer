using Clipboard.Interfaces;
using Clipboard.Services;
using CommunityToolkit.Mvvm.Messaging;
using Favorites.Messages;
using GlobalHotkeys.Interfaces;
using Hierarchy.Dto;
using Hierarchy.Enums;
using Notifications.Interfaces;
using Serilog;
using Shared.Extensions;
using Shared.Properties;
using System.Threading;
using System.Threading.Tasks;
using Threading.Interfaces;
using WindowContracts.Interfaces;

namespace GlobalHotkeys.Services;

/// <inheritdoc cref="IHotkeyContentCopier" />
public sealed class HotkeyContentCopier : ContentCopierBase, IHotkeyContentCopier
{
	#region Data
	/// <inheritdoc cref="ICopyHistoryRecorder" />
	private readonly ICopyHistoryRecorder _copyHistory;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMainWindowActivator" />
	private readonly IMainWindowActivator _mainWindowActivator;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="INotificationService" />
	private readonly INotificationService _notification;
	#endregion

	#region Constructors
	public HotkeyContentCopier(
		ICopyHistoryRecorder copyHistory,
		IDispatcherAccessor dispatcher,
		IFileTextCopier fileTextCopier,
		ILogger logger,
		IMainWindowActivator mainWindowActivator,
		IMessenger messenger,
		INotificationService notification) : base(fileTextCopier)
	{
		_copyHistory = copyHistory;

		_dispatcher = dispatcher;

		_logger = logger;

		_mainWindowActivator = mainWindowActivator;

		_messenger = messenger;

		_notification = notification;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task CopyAsync(FileDto file, CancellationToken token = default)
	{
		// The password of a locked keeper is asked for over the main window.
		if (file.EncryptionStatus == EncryptionStatus.Encrypted)
		{
			await ActivateWindowAsync().ConfigureAwait(false);
		}

		// Stryker disable once String : wording of the description
		if (!await CopyTextAsync(file, $"{Strings.CopyContent}: {file.Name}", token).ConfigureAwait(false))
		{
			return;
		}

		_notification.ShowToast(string.Format(Strings.TheContentsCopiedToClipboard, file.Name));

		await _dispatcher
			.PostAsync(() => _copyHistory.Add(file, showAtOnce: true))
			.ConfigureAwait(false);
	}

	/// <inheritdoc />
	protected override void ReportDamaged(FileDto file)
	{
		_logger.LogError($@"{Strings.FailedToLoadFileContents} of file ""{file.Id}""");
	}

	/// <inheritdoc />
	protected override void ReportEmpty(FileDto file)
	{
		_logger.LogInformation($@"{Strings.ThereIsNoContentFor} ""{file.Name}""");
	}

	/// <inheritdoc />
	protected override void ReportNotText(FileDto file)
	{
		_logger.LogWarning($@"{Strings.NonTextFileContents} of file ""{file.Id}""");

		_notification.ShowToast($@"{Strings.NonTextFileContents}: ""{file.Name}""");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Activates the main window and opens its favorites.
	/// </summary>
	private Task ActivateWindowAsync() => _dispatcher.PostAsync(() =>
	{
		_mainWindowActivator.ActivateMainWindow();

		_messenger.Send(new ShowFavoritesMessage());
	});
	#endregion
}
