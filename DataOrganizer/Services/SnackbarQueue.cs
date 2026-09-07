using Avalonia.Threading;
using DataOrganizer.Interfaces;
using DataOrganizer.Messages;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Services;

public sealed class SnackbarQueue : ISnackbarQueue
{
	#region Data
	/// <summary>
	/// Time a message stays on the screen.
	/// </summary>
	public static readonly TimeSpan MessageDuration = TimeSpan.FromSeconds(5.0);

	/// <summary>
	/// Number of messages kept waiting; the ones on top of that are dropped.
	/// </summary>
	private const int MaxWaiting = 10;

	/// <summary>
	/// Pause between a message leaving the screen and the next one taking its place.
	/// </summary>
	private static readonly TimeSpan Gap = TimeSpan.FromSeconds(0.5);

	/// <summary>
	/// Interval between the checks for a free host.
	/// </summary>
	private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(0.25);

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="ISnackbarPresenter" />
	private readonly ISnackbarPresenter _presenter;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// Messages waiting for their turn.
	/// </summary>
	private readonly Queue<ShowSnackbarMessage> _waiting = new();

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
	public SnackbarQueue(
		ILogger logger,
		ISnackbarPresenter presenter,
		TimeProvider timeProvider)
	{
		_logger = logger;

		_presenter = presenter;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool Show(ShowSnackbarMessage message)
	{
		if (!_presenter.IsHostLoaded)
		{
			return false;
		}

		if (IsHostFree())
		{
			Post(message);

			return true;
		}

		if (_waiting.Count >= MaxWaiting)
		{
			_logger.LogWarning($"Too many messages are waiting for the snackbar, dropped: {message.Text}");

			return false;
		}

		_waiting.Enqueue(message);

		StartTicking();

		return true;
	}
	#endregion

	#region Helpers
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

	/// <summary>
	/// Tells whether no message occupies the host at the moment.
	/// </summary>
	private bool IsHostFree()
	{
		return _freeAt is not { } freeAt || _timeProvider.GetUtcNow() >= freeAt;
	}

	/// <summary>
	/// Hands a message over to the host and holds the place until it goes away.
	/// </summary>
	private void Post(ShowSnackbarMessage message)
	{
		_presenter.Post(message, MessageDuration);

		_freeAt = _timeProvider.GetUtcNow() + MessageDuration + Gap;
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
	#endregion
}
