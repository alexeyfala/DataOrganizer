using Avalonia.Threading;
using DataOrganizer.Dto;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Notifications;
using Serilog;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services.Notifications;

public sealed class NotificationService : INotificationService
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// Messages shown inside the window that carries the host.
	/// </summary>
	private readonly MessageChannel<SnackbarContent> _snackbars;

	/// <summary>
	/// Messages shown in a window of their own.
	/// </summary>
	private readonly MessageChannel<string> _toasts;

	/// <summary>
	/// <c>True</c> while the tick loop is scheduled.
	/// </summary>
	private bool _isTicking;
	#endregion

	#region Constructors
	public NotificationService(
		IDispatcherAccessor dispatcher,
		ILogger logger,
		ISnackbarPresenter snackbarPresenter,
		IToastPresenter toastPresenter,
		TimeProvider timeProvider)
	{
		_dispatcher = dispatcher;

		_logger = logger;

		_snackbars = new(snackbarPresenter, timeProvider);

		_toasts = new(toastPresenter, timeProvider);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowErrorSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Error);

	/// <inheritdoc />
	public void ShowInformationSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Information);

	/// <inheritdoc />
	public void ShowToast(string message) => _dispatcher.Post(() => Show(_toasts, message));

	/// <inheritdoc />
	public void ShowWarningSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Warning);
	#endregion

	#region Helpers
	/// <summary>
	/// Lets both channels move on and stops the loop once neither has anything left to do.
	/// </summary>
	internal bool Tick()
	{
		bool hasSnackbars = _snackbars.Tick();

		bool hasToasts = _toasts.Tick();

		if (hasSnackbars || hasToasts)
		{
			return true;
		}

		_isTicking = false;

		return false;
	}

	/// <summary>
	/// Writes the message to the log at the level it is shown with.
	/// </summary>
	private void LogSnackbar(string text, SnackbarMessageLevel level, bool isShown)
	{
		string message = $"{(isShown ? "Shown in Snackbar" : "Does not shown in Snackbar")}: {text}";

		switch (level)
		{
			case SnackbarMessageLevel.Warning:
				_logger.LogWarning(message);
				break;

			case SnackbarMessageLevel.Error:
				_logger.LogError(message, breakInDebugger: false);
				break;

			default:
				_logger.LogInformation(message);
				break;
		}
	}

	/// <summary>
	/// Gives a message to its channel and keeps the loop running while the channel has work to do.
	/// </summary>
	private bool Show<TContent>(MessageChannel<TContent> channel, TContent content) where TContent : notnull
	{
		if (!channel.Show(content))
		{
			return false;
		}

		StartTicking();

		return true;
	}

	/// <summary>
	/// Shows a snackbar message with the given level.
	/// </summary>
	private void ShowSnackbar(string text, SnackbarMessageLevel level)
	{
		_dispatcher.Post(() => LogSnackbar(text, level, Show(_snackbars, new SnackbarContent(text, level))));
	}

	/// <summary>
	/// Schedules the tick loop unless it is already running.
	/// </summary>
	private void StartTicking()
	{
		if (_isTicking)
		{
			return;
		}

		_isTicking = true;

		DispatcherTimer.Run(Tick, MessageChannelOptions.PollInterval);
	}
	#endregion
}
