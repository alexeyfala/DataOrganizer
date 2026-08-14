using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Settings;
using DataOrganizer.Messages;
using Serilog;
using Shared.Extensions;
using System;

namespace DataOrganizer.Services.Encryption;

public sealed partial class AutoLockService : ObservableObject, IAutoLockService
{
	#region Properties
	/// <inheritdoc />
	public bool IsArmed => Remaining is not null;

	/// <inheritdoc />
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsArmed))]
	public partial TimeSpan? Remaining { get; private set; }
	#endregion

	#region Data
	/// <summary>
	/// Interval between the countdown updates.
	/// </summary>
	private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1.0);

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;

	/// <summary>
	/// Moment the countdown expires at; <c>null</c> while no countdown is running.
	/// </summary>
	private DateTimeOffset? _expiresAt;

	/// <summary>
	/// <c>True</c> while the tick loop is scheduled.
	/// </summary>
	private bool _isTicking;
	#endregion

	#region Constructors
	public AutoLockService(
		IAppSettingsStore settingsStore,
		ILogger logger,
		IMessenger messenger,
		TimeProvider timeProvider)
	{
		_logger = logger;

		_messenger = messenger;

		_settingsStore = settingsStore;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Arm()
	{
		int minutes = _settingsStore
			.Settings
			.AutoLockMinutes;

		if (minutes <= 0)
		{
			Stop();

			return;
		}

		TimeSpan delay = TimeSpan.FromMinutes(minutes);

		_expiresAt = _timeProvider.GetUtcNow() + delay;

		Remaining = delay;

		StartTicking();
	}

	/// <inheritdoc />
	public void Stop()
	{
		if (_expiresAt is null)
		{
			return;
		}

		_logger.LogDebug("Auto-lock countdown stopped.");

		_expiresAt = null;

		// The tick loop ends itself once the countdown is gone.
		Remaining = null;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Updates the time left and locks once it runs out; <c>False</c> ends the tick loop.
	/// </summary>
	internal bool Tick()
	{
		if (_expiresAt is not { } expiresAt)
		{
			_isTicking = false;

			return false;
		}

		TimeSpan remaining = expiresAt - _timeProvider.GetUtcNow();

		if (remaining > TimeSpan.Zero)
		{
			// Whole seconds keep a late tick from skipping a value in the countdown.
			Remaining = TimeSpan.FromSeconds(Math.Ceiling(remaining.TotalSeconds));

			return true;
		}

		_isTicking = false;

		_expiresAt = null;

		Remaining = null;

		_logger.LogInformation("Auto-lock countdown expired");

		_messenger.Send(new SessionAutoLockedMessage());

		return false;
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
	#endregion
}
