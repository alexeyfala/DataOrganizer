using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages.Clipboard;
using DataOrganizer.Models.Clipboard;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardLogService : IClipboardLogService
{
	#region Properties
	/// <inheritdoc />
	public ObservableCollection<ClipboardLogEntryBase> Entries => _history.Entries;

	/// <inheritdoc />
	public bool IsRunning => Volatile.Read(ref _isLoopRunning);
	#endregion

	#region Data
	/// <inheritdoc cref="IClipboardAutoClear" />
	private readonly IClipboardAutoClear _autoClear;

	/// <summary>
	/// Serializes a poll tick against a clear operation so a poll tick cannot
	/// insert an entry while the log / clipboard is being cleared.
	/// </summary>
	private readonly IClipboardGate _clearGate;

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="IClipboardHistory" />
	private readonly IClipboardHistory _history;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Guards the service against a double start. The loop sets this to
	/// <c>False</c> in its finally block once it exits.
	/// </summary>
	private bool _isLoopRunning;

	/// <summary>
	/// The running polling loop task, awaited on dispose so the loop has fully
	/// stopped before shared state is disposed.
	/// </summary>
	private Task? _loopTask;

	/// <summary>
	/// Linked cancellation source created on start from the user token.
	/// </summary>
	private CancellationTokenSource? _stopCts;
	#endregion

	#region Constructors
	public ClipboardLogService(
		IClipboardAccessor clipboard,
		IClipboardAutoClear autoClear,
		IClipboardGate clearGate,
		IClipboardHistory history,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		IMessenger messenger)
	{
		_clipboard = clipboard;

		_autoClear = autoClear;

		_clearGate = clearGate;

		_history = history;

		_dispatcher = dispatcher;

		_logger = logger;

		_messenger = messenger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ClearAllAsync()
	{
		return ClearCoreAsync(
			clearSystem: true,
			preservePinned: true,
			ClipboardLogChangeKind.ClearedByUser);
	}

	/// <inheritdoc />
	public Task ClearEntriesAsync()
	{
		return ClearCoreAsync(
			clearSystem: false,
			preservePinned: false,
			ClipboardLogChangeKind.ClearedOnStop);
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		Stop();

		// Wait for the polling loop to fully unwind before disposing shared state.
		if (_loopTask is not null)
		{
			try
			{
				await _loopTask.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogDebug($"Clipboard loop ended with an error during dispose: {ex.Message}");
			}
		}

		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		_clearGate.Release();
	}

	/// <inheritdoc />
	public void Merge(IReadOnlyList<ClipboardLogEntryBase> entries) => _history.Merge(entries);

	/// <inheritdoc />
	public async Task RemoveAsync(ClipboardLogEntryBase entry)
	{
		if (IsDisposed())
		{
			return;
		}

		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			bool removed = false;

			bool wasActive = false;

			// Touching Entries (and reading IsActive) must happen on the UI thread.
			await _dispatcher.PostAsync(() =>
			{
				wasActive = entry.IsActive;

				removed = _history.Remove(entry);
			}).ConfigureAwait(false);

			if (!removed)
			{
				return;
			}

			NotifyChanged(ClipboardLogChangeKind.Updated);

			NotifyEntryCountChanged();

			// Only the active entry's content actually lives in the system clipboard; empty it so the just-removed
			// content is not re-captured by the next poll tick (and is gone from a subsequent paste).
			if (wasActive)
			{
				await _clipboard
					.ClearAsync()
					.ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);
		}
		finally
		{
			_clearGate.Release();
		}
	}

	/// <inheritdoc />
	public async Task RestoreAsync(ClipboardLogEntryBase entry, bool keepPosition = false)
	{
		if (IsDisposed())
		{
			return;
		}

		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			switch (entry)
			{
				case ClipboardTextEntryBase textEntry:
					_logger.LogInformation(
						$"Restoring clipboard entry: {textEntry.GetType().Name}, {textEntry.Text.Length} chars" +
						$"{(textEntry.IsHtml ? " + HTML" : string.Empty)}" +
						$"{(textEntry.IsRtf ? " + RTF" : string.Empty)}.");

					bool isWritten = await _clipboard.SetTextWithFormatsAsync(
						textEntry.Text,
						textEntry.Html,
						textEntry.Rtf,
						textEntry.IsSensitive).ConfigureAwait(false);

					// A restored sensitive entry carries the ownership marker; arm the same auto-clear as a fresh copy.
					if (isWritten && textEntry.IsSensitive)
					{
						_autoClear.Arm();
					}
					break;

				case ClipboardImageEntry imageEntry when imageEntry.OriginalPng is { Length: > 0 } png:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardImageEntry)}, {png.Length} bytes (PNG).");

					await _clipboard
						.SetImageAsync(png)
						.ConfigureAwait(false);
					break;

				case ClipboardFilesEntry filesEntry when filesEntry.FileSystemEntries is { Count: > 0 } fileSystemEntries:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardFilesEntry)}, {fileSystemEntries.Count} items.");

					await _clipboard
						.SetFilesAsync(fileSystemEntries)
						.ConfigureAwait(false);
					break;
			}

			await _dispatcher
				.PostAsync(() => _history.MarkRestored(entry, keepPosition))
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogWarning($"Restore from clipboard log failed: {ex.Message}");
		}
		finally
		{
			_clearGate.Release();
		}
	}

	/// <inheritdoc />
	public Task StartAsync(CancellationToken token = default)
	{
		_logger.LogInformation($"{nameof(ClipboardLogService)}.{nameof(StartAsync)} requested.");

		if (IsDisposed() || Interlocked.Exchange(ref _isLoopRunning, true))
		{
			return Task.CompletedTask;
		}

		CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

		Interlocked
			.Exchange(ref _stopCts, cancellation)?
			.Dispose();

		// Kick off the loop and return once polling has started; the loop runs until Stop/Dispose.
		_loopTask = LoopAsync(cancellation);

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public void Stop()
	{
		CancellationTokenSource? cancellation = Interlocked.Exchange(ref _stopCts, null);

		if (cancellation is null)
		{
			return;
		}

		_logger.LogInformation($"{nameof(ClipboardLogService)}.{nameof(Stop)} requested.");

		try
		{
			cancellation.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Already disposed — nothing to do.
		}
		finally
		{
			cancellation.Dispose();
		}
	}

	/// <inheritdoc />
	public void TogglePin(ClipboardLogEntryBase entry) => _history.TogglePin(entry);

	/// <summary>
	/// Reads the clipboard once and inserts a new entry if the content changed.
	/// </summary>
	internal async Task PollOnceAsync()
	{
		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			bool isSensitive = await _clipboard
				.ContainsSensitivityMarkerAsync()
				.ConfigureAwait(false);

			if (await _clipboard.TryReadFilesAsync() is { Count: > 0 } fileSystemEntries)
			{
				byte[] hash = ClipboardHistory.HashFiles(fileSystemEntries);

				await _dispatcher
					.PostAsync(() => _history.HandleNewPayload(hash, () => ClipboardLogEntryFactory.CreateFiles(fileSystemEntries, hash), isSensitive))
					.ConfigureAwait(false);

				return;
			}

			if (await _clipboard.TryReadImagePngAsync() is { Length: > 0 } png)
			{
				byte[] hash = SHA256.HashData(png);

				await _dispatcher
					.PostAsync(() => _history.HandleNewPayload(hash, () => ClipboardLogEntryFactory.CreateImage(png, hash), isSensitive))
					.ConfigureAwait(false);

				return;
			}

			if (await _clipboard.TryReadTextAsync() is { Length: > 0 } text)
			{
				// Read companion formats up front so the hash reflects them: the same text copied as
				// plain (Notepad) vs formatted (Word) is genuinely different and must not be deduped.
				string? html = await _clipboard
					.TryReadHtmlAsync()
					.ConfigureAwait(false);

				string? rtf = await _clipboard
					.TryReadRtfAsync()
					.ConfigureAwait(false);

				byte[] hash = ClipboardHistory.ComputeTextEntryHash(text, html, rtf);

				await _dispatcher
					.PostAsync(() => _history.HandleNewPayload(hash, () => ClipboardLogEntryFactory.CreateText(text, html, rtf, hash), isSensitive))
					.ConfigureAwait(false);

				return;
			}

			// Nothing capturable on the clipboard (e.g. emptied via Win+V "Clear all") — drop the stale highlight.
			await _dispatcher
				.PostAsync(_history.HandleClipboardCleared)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"Clipboard poll failed: {ex.Message}");
		}
		finally
		{
			_clearGate.Release();
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Clears <see cref="Entries" /> and forgets the last observed payload.
	/// </summary>
	private async Task ClearCoreAsync(
		bool clearSystem,
		bool preservePinned,
		ClipboardLogChangeKind clearKind)
	{
		if (IsDisposed())
		{
			return;
		}

		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			int remaining = 0;

			// Touching Entries (and reading Count for the log) must happen on the UI thread.
			await _dispatcher.PostAsync(() =>
			{
				_logger.LogInformation(
					$"Clearing clipboard log ({Entries.Count} entries)" +
					$"{(clearSystem ? " and emptying the system clipboard" : " without touching the system clipboard")}.");

				remaining = _history.Clear(preservePinned);
			}).ConfigureAwait(false);

			ClipboardLogChangeKind effectiveKind = remaining > 0
				? ClipboardLogChangeKind.Updated
				: clearKind;

			NotifyChanged(effectiveKind);

			NotifyEntryCountChanged();

			if (!clearSystem)
			{
				return;
			}

			// Emptying the OS clipboard too, so the just-cleared content is not re-captured
			// by the next poll tick (it only reappears on a genuine new copy).
			await _clipboard
				.ClearAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);
		}
		finally
		{
			_clearGate.Release();
		}
	}

	/// <summary>
	/// <c>True</c> while the service is not disposed and the operation is not cancelled.
	/// </summary>
	private bool IsActive(CancellationToken token) => !IsDisposed() && !token.IsCancellationRequested;

	/// <summary>
	/// <c>True</c> after the service has been disposed.
	/// </summary>
	private bool IsDisposed() => Volatile.Read(ref _isDisposed);

	/// <summary>
	/// Polling loop.
	/// </summary>
	private async Task LoopAsync(CancellationTokenSource cancellation)
	{
		CancellationToken token = cancellation.Token;

		TimeSpan pollInterval = TimeSpan.FromMilliseconds(750.0);

		_logger.LogInformation(
			$"{nameof(ClipboardLogService)} loop started (interval = {pollInterval.TotalMilliseconds:F0} ms, limit = {ClipboardHistory.LogLimit}).");

		try
		{
			while (IsActive(token))
			{
				await Task
					.Delay(pollInterval, token)
					.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

				if (!IsActive(token))
				{
					break;
				}

				await PollOnceAsync().ConfigureAwait(true);
			}
		}
		finally
		{
			Interlocked.Exchange(ref _isLoopRunning, false);

			Interlocked.CompareExchange(ref _stopCts, null, cancellation);

			cancellation.Dispose();

			_logger.LogInformation($"{nameof(ClipboardLogService)} loop stopped.");
		}
	}

	/// <summary>
	/// Publishes a <see cref="ClipboardLogChangedMessage" /> so listeners can react to a change.
	/// </summary>
	private void NotifyChanged(ClipboardLogChangeKind kind) => _messenger.Send(new ClipboardLogChangedMessage(kind));

	/// <summary>
	/// Publishes a <see cref="ClipboardLogEntryCountChangedMessage" /> after entries are added or removed.
	/// </summary>
	private void NotifyEntryCountChanged() => _messenger.Send(new ClipboardLogEntryCountChangedMessage());
	#endregion
}
