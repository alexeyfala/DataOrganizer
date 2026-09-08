using Avalonia;
using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using DataOrganizer.ViewModels;
using DataOrganizer.Windows;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class NotificationService : INotificationService
{
	#region Data
	/// <summary>
	/// Time a message stays on the screen, the same for a toast and for a snackbar.
	/// </summary>
	internal static readonly TimeSpan MessageDuration = TimeSpan.FromSeconds(4.0);

	/// <summary>
	/// Number of messages kept waiting; the ones on top of that are dropped.
	/// </summary>
	private const int MaxWaitingSnackbars = 10;

	/// <summary>
	/// Pause between a message leaving the screen and the next one taking its place.
	/// </summary>
	private static readonly TimeSpan SnackbarGap = TimeSpan.FromSeconds(0.2);

	/// <summary>
	/// Interval between the checks for a free host.
	/// </summary>
	private static readonly TimeSpan SnackbarTickInterval = TimeSpan.FromSeconds(0.25);

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISnackbarPresenter" />
	private readonly ISnackbarPresenter _snackbarPresenter;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <inheritdoc cref="IViewFactory" />
	private readonly IViewFactory _viewFactory;

	/// <summary>
	/// Messages waiting for their turn.
	/// </summary>
	private readonly Queue<SnackbarContent> _waitingSnackbars = new();

	/// <summary>
	/// <c>True</c> while the tick loop is scheduled.
	/// </summary>
	private bool _isSnackbarTicking;

	/// <summary>
	/// Moment the shown message frees the host at; <c>null</c> while nothing is shown.
	/// </summary>
	private DateTimeOffset? _snackbarFreeAt;
	#endregion

	#region Constructors
	public NotificationService(
		Application app,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		ISnackbarPresenter snackbarPresenter,
		IViewFactory viewFactory,
		TimeProvider timeProvider)
	{
		_app = app;

		_dispatcher = dispatcher;

		_logger = logger;

		_snackbarPresenter = snackbarPresenter;

		_timeProvider = timeProvider;

		_viewFactory = viewFactory;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowErrorSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Error);

	/// <inheritdoc />
	public void ShowInformationSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Information);

	/// <inheritdoc />
	public void ShowToast(string message) => _dispatcher.Post(async () =>
	{
		try
		{
			_app
				.FindWindow<ToastWindow>()?
				.Close();

			ToastViewModel viewModel = _viewFactory.CreateViewModel<ToastViewModel>();

			ToastWindow window = _viewFactory.CreateWindow<ToastWindow>(viewModel);

			viewModel.Title = AppUtils.AppNameParted;

			viewModel.Message = message;

			if (window
				.Screens
				.Primary is not { } screen)
			{
				return;
			}

			window.Show();

			PixelSize screenSize = screen
				.WorkingArea
				.Size;

			PixelSize windowSize = PixelSize.FromSize(window.ClientSize, screen.Scaling);

			const int margin = 10;

			window.Position = new PixelPoint(
				screenSize.Width - (windowSize.Width + margin),
				screenSize.Height - (windowSize.Height + margin));

			await Task
				.Delay(MessageDuration)
				.ConfigureAwait(true);

			while (window.IsPointerOver)
			{
				await Task
					.Delay(TimeSpan.FromSeconds(1))
					.ConfigureAwait(true);
			}

			window.Close();
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	});

	/// <inheritdoc />
	public void ShowWarningSnackbar(string text) => ShowSnackbar(text, SnackbarMessageLevel.Warning);
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the next waiting message once the host is free, and stops the loop when none are left
	/// or the host is gone.
	/// </summary>
	internal bool TickSnackbars()
	{
		if (!_snackbarPresenter.IsHostLoaded)
		{
			_waitingSnackbars.Clear();

			return StopSnackbarTicking();
		}

		if (!IsSnackbarHostFree())
		{
			return true;
		}

		if (_waitingSnackbars.Count == 0)
		{
			return StopSnackbarTicking();
		}

		PostSnackbar(_waitingSnackbars.Dequeue());

		return true;
	}

	/// <summary>
	/// Puts a message in the queue and returns <c>false</c> when it will not be shown at all.
	/// </summary>
	private bool EnqueueSnackbar(SnackbarContent content)
	{
		if (!_snackbarPresenter.IsHostLoaded)
		{
			return false;
		}

		if (IsSnackbarHostFree())
		{
			PostSnackbar(content);

			return true;
		}

		if (_waitingSnackbars.Count >= MaxWaitingSnackbars)
		{
			return false;
		}

		_waitingSnackbars.Enqueue(content);

		StartSnackbarTicking();

		return true;
	}

	/// <summary>
	/// Tells whether no message occupies the host at the moment.
	/// </summary>
	private bool IsSnackbarHostFree()
	{
		return _snackbarFreeAt is not { } freeAt || _timeProvider.GetUtcNow() >= freeAt;
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
				_logger.LogError(message, assertDebug: false);
				break;

			default:
				_logger.LogInformation(message);
				break;
		}
	}

	/// <summary>
	/// Hands a message over to the host and holds the place until it goes away.
	/// </summary>
	private void PostSnackbar(SnackbarContent content)
	{
		_snackbarPresenter.Post(content, MessageDuration);

		_snackbarFreeAt = _timeProvider.GetUtcNow() + MessageDuration + SnackbarGap;
	}

	/// <summary>
	/// Shows a snackbar message with the given level.
	/// </summary>
	private void ShowSnackbar(string text, SnackbarMessageLevel level)
	{
		_dispatcher.Post(() => LogSnackbar(text, level, EnqueueSnackbar(new(text, level))));
	}

	/// <summary>
	/// Schedules the tick loop unless it is already running.
	/// </summary>
	private void StartSnackbarTicking()
	{
		if (_isSnackbarTicking)
		{
			return;
		}

		_isSnackbarTicking = true;

		DispatcherTimer.Run(TickSnackbars, SnackbarTickInterval);
	}

	/// <summary>
	/// Frees the host and reports that the loop is over.
	/// </summary>
	private bool StopSnackbarTicking()
	{
		_snackbarFreeAt = null;

		_isSnackbarTicking = false;

		return false;
	}
	#endregion
}
