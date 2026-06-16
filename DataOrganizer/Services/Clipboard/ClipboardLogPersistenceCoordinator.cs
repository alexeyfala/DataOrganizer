using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages;
using Serilog;
using Shared.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardLogPersistenceCoordinator :
	IClipboardLogPersistenceCoordinator,
	IRecipient<ClipboardLogChangedMessage>
{
	#region Data
	/// <summary>
	/// Default delay after the last change before writing the encrypted journal,
	/// coalescing bursts of clipboard activity into a single save.
	/// </summary>
	private static readonly TimeSpan DefaultSaveDebounce = TimeSpan.FromMilliseconds(1500.0);

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// Effective debounce delay for this instance (overridable in tests).
	/// </summary>
	private readonly TimeSpan _saveDebounce;

	/// <inheritdoc cref="IAppSettingsManager" />
	private readonly IAppSettingsManager _settingsManager;

	/// <inheritdoc cref="IClipboardLogStore" />
	private readonly IClipboardLogStore _store;

	/// <inheritdoc cref="IClipboardLogService" />
	private readonly IClipboardLogService _сlipboardLog;

	/// <summary>
	/// <c>True</c> once <see cref="Start" /> has subscribed to change notifications.
	/// </summary>
	private bool _isStarted;

	/// <summary>
	/// Cancellation source for the pending debounced save.
	/// </summary>
	private CancellationTokenSource? _pendingSaveCts;
	#endregion

	#region Constructors
	public ClipboardLogPersistenceCoordinator(
		IAppSettingsManager settingsManager,
		IClipboardLogService сlipboardLog,
		IClipboardLogStore store,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		IMessenger messenger) : this(
			  settingsManager,
			  сlipboardLog,
			  store,
			  dispatcher,
			  logger,
			  messenger,
			  DefaultSaveDebounce)
	{
	}

	/// <summary>
	/// Test constructor that allows overriding the debounce delay.
	/// </summary>
	internal ClipboardLogPersistenceCoordinator(
		IAppSettingsManager settingsManager,
		IClipboardLogService сlipboardLog,
		IClipboardLogStore store,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		IMessenger messenger,
		TimeSpan saveDebounce)
	{
		_dispatcher = dispatcher;

		_logger = logger;

		_messenger = messenger;

		_settingsManager = settingsManager;

		_store = store;

		_сlipboardLog = сlipboardLog;

		_saveDebounce = saveDebounce;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public bool RequiresUnlock => _settingsManager.Settings.PersistClipboardHistory && !_store.IsUnlocked;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void DisablePersistence()
	{
		CancelPendingSave();

		_store.EraseAll();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_messenger.UnregisterAll(this);

		CancelPendingSave();

		await FlushAsync().ConfigureAwait(false);
	}

	/// <inheritdoc />
	public void Receive(ClipboardLogChangedMessage message)
	{
		switch (message.Kind)
		{
			case ClipboardLogChangeKind.Updated:
				if (_store.IsUnlocked)
				{
					ScheduleSave();
				}
				break;

			case ClipboardLogChangeKind.ClearedByUser:
				// Explicit clear: drop the pending save and erase the journal (keep the wrapped key).
				CancelPendingSave();

				if (_store.IsUnlocked)
				{
					_store.EraseHistory();
				}
				break;

			case ClipboardLogChangeKind.ClearedForStop:
				// Tracking toggled off: drop the pending save but keep the saved history on disk.
				CancelPendingSave();
				break;
		}
	}

	/// <inheritdoc />
	public void Start()
	{
		if (_isStarted)
		{
			return;
		}

		_isStarted = true;

		_messenger.RegisterAll(this);

		_logger.LogInformation(
			$"{nameof(ClipboardLogPersistenceCoordinator)} started ({nameof(RequiresUnlock)} = {RequiresUnlock}).");
	}

	/// <inheritdoc />
	public async Task<ClipboardLogStatus> TryUnlockAndMergeAsync(
		byte[] password,
		CancellationToken token = default)
	{
		ClipboardLogUnlockResult result = await _store
			.TryUnlockAsync(password, token)
			.ConfigureAwait(false);

		if (result.Status != ClipboardLogStatus.Unlocked)
		{
			return result.Status;
		}

		// Merge previous-session entries on the UI thread (Merge raises no notification),
		// then write the merged set once.
		await _dispatcher
			.PostAsync(() => _сlipboardLog.Merge(result.Entries))
			.ConfigureAwait(false);

		await SaveSnapshotAsync(token).ConfigureAwait(false);

		return ClipboardLogStatus.Unlocked;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Cancels the pending debounced save, if any.
	/// </summary>
	private void CancelPendingSave()
	{
		CancellationTokenSource? pending = Interlocked.Exchange(ref _pendingSaveCts, null);

		try
		{
			pending?.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Already completed and disposed — nothing to do.
		}
	}

	/// <summary>
	/// Forces an immediate save of the current entries when persistence is unlocked.
	/// </summary>
	private async Task FlushAsync()
	{
		if (!_store.IsUnlocked)
		{
			return;
		}

		await SaveSnapshotAsync(CancellationToken.None).ConfigureAwait(false);
	}

	/// <summary>
	/// Runs the debounce delay, then saves; cancellation (a newer change or clear) drops the save.
	/// </summary>
	private async Task RunDebouncedSaveAsync(CancellationTokenSource cancellation)
	{
		try
		{
			await Task
				.Delay(_saveDebounce, cancellation.Token)
				.ConfigureAwait(false);

			await SaveSnapshotAsync(cancellation.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			// Superseded by a newer change or cancelled by clear / dispose.
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
		finally
		{
			Interlocked.CompareExchange(ref _pendingSaveCts, null, cancellation);

			cancellation.Dispose();
		}
	}

	/// <summary>
	/// Snapshots the service entries on the UI thread and writes the encrypted journal.
	/// </summary>
	private async Task SaveSnapshotAsync(CancellationToken token)
	{
		if (!_store.IsUnlocked)
		{
			return;
		}

		ClipboardLogEntryBase[] snapshot = await _dispatcher
			.PostAsync(() => _сlipboardLog.Entries.ToArray())
			.ConfigureAwait(false);

		await _store
			.SaveAsync(snapshot, token)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Cancels any pending save and starts a fresh debounced one.
	/// </summary>
	private void ScheduleSave()
	{
		CancellationTokenSource cancellation = new();

		CancellationTokenSource? previous = Interlocked.Exchange(ref _pendingSaveCts, cancellation);

		try
		{
			previous?.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Already completed and disposed — nothing to do.
		}

		_ = RunDebouncedSaveAsync(cancellation);
	}
	#endregion
}
