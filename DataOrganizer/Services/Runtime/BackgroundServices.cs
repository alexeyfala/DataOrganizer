using DataOrganizer.Dto.Settings;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Interfaces.Diagnostics;
using DataOrganizer.Interfaces.Encryption;
using DataOrganizer.Interfaces.Hotkeys;
using DataOrganizer.Interfaces.Runtime;
using DataOrganizer.Interfaces.Settings;
using Hierarchy.Enums;
using Hierarchy.Extensions;
using Shared.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Runtime;

/// <summary>
/// The one place that starts, adjusts and stops the hotkey tracking, the clipboard history with its saving and the
/// auto-lock countdown.
/// </summary>
public sealed class BackgroundServices : IAutoLockControl, IBackgroundServices, IHotkeyTrackingControl, ISettingsApplier
{
	#region Data
	/// <inheritdoc cref="IAutoLockService" />
	private readonly IAutoLockService _autoLock;

	/// <inheritdoc cref="IClipboardLogRunner" />
	private readonly IClipboardLogRunner _clipboardLog;

	/// <inheritdoc cref="IClipboardLogPersistenceCoordinator" />
	private readonly IClipboardLogPersistenceCoordinator _clipboardLogPersistence;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="IKeyboardInputHook" />
	private readonly Lazy<IKeyboardInputHook> _keyboardInputHook;

	/// <inheritdoc cref="ISessionHierarchy" />
	private readonly ISessionHierarchy _sessionHierarchy;

	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;
	#endregion

	#region Constructors
	public BackgroundServices(
		IAppSettingsStore settingsStore,
		IAutoLockService autoLock,
		IClipboardLogPersistenceCoordinator clipboardLogPersistence,
		IClipboardLogRunner clipboardLog,
		ISessionHierarchy sessionHierarchy,
		ITaskExceptionHandler exceptionHandler,
		Lazy<IKeyboardInputHook> keyboardInputHook)
	{
		_autoLock = autoLock;

		_clipboardLog = clipboardLog;

		_clipboardLogPersistence = clipboardLogPersistence;

		_exceptionHandler = exceptionHandler;

		_keyboardInputHook = keyboardInputHook;

		_sessionHierarchy = sessionHierarchy;

		_settingsStore = settingsStore;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ApplySettingsAsync(
		AppSettings settings,
		bool wasPersistingClipboard,
		int previousAutoLockMinutes,
		CancellationToken token = default)
	{
		// A changed delay takes effect at once: the countdown restarts from it, and no auto-lock stops it.
		if (settings.AutoLockMinutes != previousAutoLockMinutes)
		{
			RefreshAutoLock();
		}

		await ApplyClipboardHistorySettingAsync(
			settings.TrackClipboardHistory,
			token).ConfigureAwait(false);

		if (wasPersistingClipboard && (!settings.PersistClipboardHistory || !settings.TrackClipboardHistory))
		{
			_clipboardLogPersistence.DisablePersistence();
		}

		await PauseHotkeyTrackingAsync(token).ConfigureAwait(false);

		if (!settings.TrackHotkeys)
		{
			return;
		}

		_exceptionHandler.Watch(_keyboardInputHook.Value.StartTrackingAsync(token));
	}

	/// <inheritdoc />
	public Task PauseHotkeyTrackingAsync() => PauseHotkeyTrackingAsync(default);

	/// <inheritdoc />
	public void RefreshAutoLock()
	{
		if (_sessionHierarchy.Hierarchy.ContainsBy(static x => x.EncryptionStatus == EncryptionStatus.Decrypted))
		{
			_autoLock.Arm();
		}
		else
		{
			_autoLock.Stop();
		}
	}

	/// <inheritdoc />
	public void RestartAutoLock() => _autoLock.Arm();

	/// <inheritdoc />
	public void ResumeHotkeyTracking()
	{
		if (!_settingsStore
			.Settings
			.TrackHotkeys)
		{
			return;
		}

		_exceptionHandler.Watch(_keyboardInputHook.Value.StartTrackingAsync());
	}

	/// <inheritdoc />
	public void Start(CancellationToken token = default)
	{
		_clipboardLogPersistence.Start();

		AppSettings settings = _settingsStore.Settings;

		if (settings.TrackClipboardHistory)
		{
			_exceptionHandler.Watch(_clipboardLog.StartAsync(token));
		}

		if (settings.IsDefault() || !settings.TrackHotkeys)
		{
			return;
		}

		_exceptionHandler.Watch(_keyboardInputHook.Value.StartTrackingAsync(token));
	}

	/// <inheritdoc />
	public async Task StopAsync()
	{
		_autoLock.Stop();

		await PauseHotkeyTrackingAsync(default).ConfigureAwait(true);

		if (!_clipboardLog.IsRunning)
		{
			return;
		}

		_clipboardLog.Stop();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Starts or stops clipboard history tracking to match <paramref name="isEnabled" />, dropping the in-memory history
	/// when disabled.
	/// </summary>
	private async Task ApplyClipboardHistorySettingAsync(bool isEnabled, CancellationToken token)
	{
		if (isEnabled)
		{
			if (!_clipboardLog.IsRunning)
			{
				_exceptionHandler.Watch(_clipboardLog.StartAsync(token));
			}

			return;
		}

		if (_clipboardLog.IsRunning)
		{
			_clipboardLog.Stop();
		}

		await _clipboardLog
			.ClearEntriesAsync()
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Stops the hotkey tracking while it runs.
	/// </summary>
	private async Task PauseHotkeyTrackingAsync(CancellationToken token)
	{
		if (!_keyboardInputHook.IsValueCreated || !_keyboardInputHook.Value.IsRunning)
		{
			return;
		}

		await _keyboardInputHook
			.Value
			.StopTrackingAsync(token)
			.ConfigureAwait(false);
	}
	#endregion
}
