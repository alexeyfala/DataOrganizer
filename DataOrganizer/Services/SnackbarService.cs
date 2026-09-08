using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Services;

public sealed class SnackbarService : ISnackbarService
{
	#region Data
	/// <summary>
	/// Time a message stays on the screen.
	/// </summary>
	public static readonly TimeSpan MessageDuration = TimeSpan.FromSeconds(4.0);

	/// <summary>
	/// Pause between a message leaving the screen and the next one taking its place.
	/// </summary>
	private static readonly TimeSpan Gap = TimeSpan.FromSeconds(0.2);

	/// <summary>
	/// Interval between the checks for a free host.
	/// </summary>
	private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(0.25);

	/// <summary>
	/// Number of messages kept waiting; the ones on top of that are dropped.
	/// </summary>
	private const int MaxWaiting = 10;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISnackbarPresenter" />
	private readonly ISnackbarPresenter _presenter;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// Messages waiting for their turn.
	/// </summary>
	private readonly Queue<SnackbarContent> _waiting = new();

	/// <summary>
	/// Moment the shown message frees the host at; <c>null</c> while nothing is shown.
	/// </summary>
	private DateTimeOffset? _freeAt;

	/// <summary>
	/// <c>True</c> while the tick loop is scheduled.
	/// </summary>
	private bool _isTicking;
	#endregion

	#region Constructors
	public SnackbarService(
		IDispatcherAccessor dispatcher,
		ILogger logger,
		ISnackbarPresenter presenter,
		TimeProvider timeProvider)
	{
		_dispatcher = dispatcher;

		_logger = logger;

		_presenter = presenter;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void ShowError(string text) => Show(text, SnackbarMessageLevel.Error);

	/// <inheritdoc />
	public void ShowInformation(string text) => Show(text, SnackbarMessageLevel.Information);

	/// <inheritdoc />
	public void ShowWarning(string text) => Show(text, SnackbarMessageLevel.Warning);
	#endregion

	#region Helpers
	/// <summary>
	/// Puts a message in the queue and returns <c>false</c> when it will not be shown at all.
	/// </summary>
	private bool Enqueue(SnackbarContent content)
	{
		if (!_presenter.IsHostLoaded)
		{
			return false;
		}

		if (IsHostFree())
		{
			Post(content);

			return true;
		}

		if (_waiting.Count >= MaxWaiting)
		{
			return false;
		}

		_waiting.Enqueue(content);

		StartTicking();

		return true;
	}

	/// <summary>
	/// Tells whether no message occupies the host at the moment.
	/// </summary>
	private bool IsHostFree()
	{
		return _freeAt is not { } freeAt || _timeProvider.GetUtcNow() >= freeAt;
	}

	/// <summary>
	/// Writes the message to the log at the level it is shown with.
	/// </summary>
	private void Log(string text, SnackbarMessageLevel level, bool isShown)
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
	private void Post(SnackbarContent content)
	{
		_presenter.Post(content, MessageDuration);

		_freeAt = _timeProvider.GetUtcNow() + MessageDuration + Gap;
	}

	/// <summary>
	/// Shows a message with the given level.
	/// </summary>
	private void Show(string text, SnackbarMessageLevel level)
	{
		_dispatcher.Post(() => Log(text, level, Enqueue(new(text, level))));
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

		DispatcherTimer.Run(Tick, TickInterval);
	}

	/// <summary>
	/// Frees the host and reports that the loop is over.
	/// </summary>
	private bool Stop()
	{
		_freeAt = null;

		_isTicking = false;

		return false;
	}

	/// <summary>
	/// Shows the next waiting message once the host is free, and stops the loop when none are left
	/// or the host is gone.
	/// </summary>
	internal bool Tick()
	{
		if (!_presenter.IsHostLoaded)
		{
			_waiting.Clear();

			return Stop();
		}

		if (!IsHostFree())
		{
			return true;
		}

		if (_waiting.Count == 0)
		{
			return Stop();
		}

		Post(_waiting.Dequeue());

		return true;
	}
	#endregion
}
