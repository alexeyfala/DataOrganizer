using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using Cysharp.Text;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Enums.Clipboard;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Interfaces;
using DataOrganizer.Interfaces.Clipboard;
using DataOrganizer.Messages;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardLogService : IClipboardLogService
{
	#region Properties
	/// <inheritdoc />
	public ObservableCollection<ClipboardLogEntryBase> Entries { get; } = [];

	/// <inheritdoc />
	public bool IsRunning => Volatile.Read(ref _isLoopRunning);
	#endregion

	#region Data
	/// <summary>
	/// Maximum number of entries kept in log.
	/// </summary>
	private const int LogLimit = 100;

	/// <summary>
	/// Linux-only byte[] format that file managers (GNOME/Nautilus and most DEs) require to paste files:
	/// first line is the operation ("copy"), then one file:// URI per line. Avalonia only advertises
	/// text/uri-list, which file managers ignore for paste — so we add this format ourselves.
	/// </summary>
	private static readonly Lazy<DataFormat<byte[]>?> GnomeCopiedFilesFormat = new(GetGnomeCopiedFilesFormat);

	/// <summary>
	/// Platform byte[] format for HTML.
	/// </summary>
	private static readonly DataFormat<byte[]>? HtmlFormat = GetHtmlFormat();

	/// <summary>
	/// Platform byte[] format for RTF.
	/// </summary>
	private static readonly DataFormat<byte[]>? RtfFormat = GetRtfFormat();

	/// <summary>
	/// Clipboard format identifiers (Windows / Linux / macOS) that password managers set to flag
	/// content as sensitive; presence of any makes us skip the entry, like the Windows Win+V history.
	/// </summary>
	private static readonly FrozenSet<string> SensitivityMarkerIdentifiers = new[]
	{
		ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing,
		ClipboardSensitivityMarkers.ClipboardViewerIgnore,
		ClipboardSensitivityMarkers.KdePasswordManagerHint,
		ClipboardSensitivityMarkers.NsPasteboardConcealedType
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Matches only when the entire trimmed text is an http(s) URL (used to pick
	/// <see cref="ClipboardUrlEntry" /> over <see cref="ClipboardTextEntry" />).
	/// </summary>
	private static readonly Regex WholeStringUrlRegex = ClipboardLogMapper.WholeStringUrlRegex();

	/// <summary>
	/// Serializes a poll tick against a clear operation so a poll tick cannot
	/// insert an entry while the log / clipboard is being cleared.
	/// </summary>
	private readonly SemaphoreSlim _clearGate = new(1, 1);

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="IStorageAccessor" />
	private readonly IStorageAccessor _storage;

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
	/// Hash of the most recently observed clipboard payload.
	/// Accessed on the UI thread only.
	/// </summary>
	private byte[]? _lastHash;

	/// <summary>
	/// The running polling loop task, awaited on dispose so the loop has fully
	/// stopped before shared state is disposed.
	/// </summary>
	private Task? _loopTask;

	/// <summary>
	/// The entry just restored to the system clipboard, awaiting re-baseline to the clipboard's
	/// actual representation on the next poll tick. UI-thread only.
	/// </summary>
	private ClipboardLogEntryBase? _restoredEntry;

	/// <summary>
	/// Linked cancellation source created on start from the user token.
	/// </summary>
	private CancellationTokenSource? _stopCts;
	#endregion

	#region Constructors
	public ClipboardLogService(
		IClipboardAccessor clipboard,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		IMessenger messenger,
		IStorageAccessor storage)
	{
		_clipboard = clipboard;

		_dispatcher = dispatcher;

		_logger = logger;

		_messenger = messenger;

		_storage = storage;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ClearAsync()
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
			ClipboardLogChangeKind.ClearedForStop);
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

		_clearGate.Dispose();
	}

	/// <inheritdoc />
	public void Merge(IReadOnlyList<ClipboardLogEntryBase> entries)
	{
		try
		{
			// Pinned go atop (exempt from the cap), unpinned are appended (capped) — keeps the pinned-atop
			// invariant. Does not raise a change notification — the caller decides whether to persist.
			foreach (ClipboardLogEntryBase entry in entries)
			{
				if (Entries.FirstOrDefault(x => HashEquals(x.Hash, entry.Hash)) is { } existing)
				{
					// Promote the in-memory copy if the saved one was pinned; preserves the pin dedup used to drop.
					if (entry.IsPinned && !existing.IsPinned)
					{
						// Read before flipping: the new pin appends to the end of the pinned block.
						int target = GetPinnedCount();

						existing.IsPinned = true;

						Entries.Move(Entries.IndexOf(existing), target);
					}

					continue;
				}

				if (entry.IsPinned)
				{
					Entries.Insert(GetPinnedCount(), entry);

					continue;
				}

				if (Entries.Count - GetPinnedCount() >= LogLimit)
				{
					continue;
				}

				Entries.Add(entry);
			}
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

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
				int index = Entries.IndexOf(entry);

				if (index < 0)
				{
					return;
				}

				removed = true;

				wasActive = entry.IsActive;

				Entries.RemoveAt(index);

				// Removing the entry currently on the system clipboard: forget the change-detection baseline so
				// the emptied clipboard is not mistaken for the previous payload on the next poll tick.
				if (wasActive)
				{
					_lastHash = null;
				}

				// Forget the entry if it was awaiting its restore echo, so a later poll does not re-baseline it.
				if (ReferenceEquals(_restoredEntry, entry))
				{
					_restoredEntry = null;
				}
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
			_logger.LogException(ex, assertDebug: false);
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
				case ClipboardTextEntry textEntry:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardTextEntry)}, {textEntry.Text.Length} chars" +
						$"{(textEntry.IsHtml ? " + HTML" : string.Empty)}" +
						$"{(textEntry.IsRtf ? " + RTF" : string.Empty)}.");

					await SetTextWithFormatsAsync(
						textEntry.Text,
						textEntry.Html,
						textEntry.Rtf,
						textEntry.IsSensitive).ConfigureAwait(false);
					break;

				case ClipboardImageEntry imageEntry when imageEntry.OriginalPng is { Length: > 0 } png:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardImageEntry)}, {png.Length} bytes (PNG).");

					await SetImageAsync(png).ConfigureAwait(false);
					break;

				case ClipboardFilesEntry filesEntry when filesEntry.FileSystemEntries is { Count: > 0 } fileSystemEntries:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardFilesEntry)}, {fileSystemEntries.Count} items.");

					await SetFilesAsync(fileSystemEntries).ConfigureAwait(false);
					break;
			}

			await _dispatcher.PostAsync(() =>
			{
				_lastHash = entry.Hash;

				_restoredEntry = entry;

				// With a pinned-open window, only highlight so entries don't jump under the user.
				if (keepPosition)
				{
					MarkActive(entry);
				}
				else
				{
					MoveToTop(entry);
				}
			}).ConfigureAwait(false);
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
		_logger.LogInformation($"{nameof(ClipboardLogService)}.{nameof(Stop)} requested.");

		CancellationTokenSource? local = Interlocked.Exchange(ref _stopCts, null);

		if (local is null)
		{
			return;
		}

		try
		{
			local.Cancel();
		}
		catch (ObjectDisposedException)
		{
			// Already disposed — nothing to do.
		}
		finally
		{
			local.Dispose();
		}
	}

	/// <inheritdoc />
	public void TogglePin(ClipboardLogEntryBase entry)
	{
		try
		{
			int index = Entries.IndexOf(entry);

			if (index < 0)
			{
				return;
			}

			bool pin = !entry.IsPinned;

			// Read before flipping: once unpinned in place, PinnedCount would stop at this entry and read 0.
			int pinnedCount = GetPinnedCount();

			entry.IsPinned = pin;

			// Pinning appends to the end of the pinned block; unpinning drops just below the remaining pins.
			int target = pin ? pinnedCount : pinnedCount - 1;

			if (index != target)
			{
				Entries.Move(index, target);
			}

			NotifyChanged(ClipboardLogChangeKind.Updated);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
		}
	}

	/// <summary>
	/// Builds a text entry, choosing <see cref="ClipboardUrlEntry" /> when the text is a whole URL.
	/// </summary>
	internal static ClipboardLogEntryBase BuildTextEntry(
		string text,
		string? html,
		string? rtf,
		byte[] hash)
	{
		string? url = TryDetectUrl(text);

		return url is null
			? new ClipboardTextEntry
			{
				Text = text,
				Html = html,
				Rtf = rtf,
				Hash = hash
			}
			: new ClipboardUrlEntry
			{
				Text = text,
				Html = html,
				Rtf = rtf,
				Url = url,
				Hash = hash
			};
	}

	/// <summary>
	/// Hashes a text payload together with whether it carries companion formats, so the same text
	/// copied as plain (e.g. Notepad) and as formatted (e.g. Word) is treated as two distinct entries.
	/// Trade-off: identical text with only different HTML/RTF formatting hashes the same and is deduped.
	/// </summary>
	internal static byte[] ComputeTextEntryHash(
		string text,
		string? html,
		string? rtf)
	{
		using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

		hash.AppendData(TextHelper
			.Utf8Encoding
			.GetBytes(text));

		// Only the presence of companion formats — not their payloads, which Office can re-render
		// non-deterministically per read (delayed rendering) and would change the hash every poll tick.
		hash.AppendData([(byte)((html is null ? 0 : 1) | (rtf is null ? 0 : 2))]);

		return hash.GetHashAndReset();
	}

	/// <summary>
	/// Hashes the file list, including kind marker so a folder and a file with the same
	/// path don't collide. Order-sensitive — selection order is preserved by the OS.
	/// </summary>
	internal static byte[] HashFiles(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		foreach (ClipboardFileSystemEntry entry in entries)
		{
			builder.Append(entry.IsFolder ? 'D' : 'F');

			builder.Append('|');

			builder.Append(entry.Path);

			builder.Append('\0');
		}

		return ComputeHash(TextHelper.Utf8Encoding.GetBytes(builder.ToString()));
	}

	/// <summary>
	/// Handles a freshly observed payload.
	/// </summary>
	internal void HandleNewPayload(
		byte[] hash,
		Func<ClipboardLogEntryBase> entryFactory,
		bool isSensitive)
	{
		if (_restoredEntry is { } restored)
		{
			_restoredEntry = null;

			_lastHash = hash;

			if (!HashEquals(restored.Hash, hash))
			{
				RebaselineRestored(restored, entryFactory());
			}

			return;
		}

		if (IsEchoOrUnchanged(hash))
		{
			return;
		}

		if (isSensitive)
		{
			_logger.LogWarning("Skipping clipboard entry: a sensitivity marker was detected.");

			return;
		}

		InsertOrMoveToTop(hash, entryFactory);
	}

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
			bool isSensitive = await ContainsSensitivityMarkerAsync().ConfigureAwait(false);

			if (await TryReadFilesAsync() is { Count: > 0 } fileSystemEntries)
			{
				byte[] hash = HashFiles(fileSystemEntries);

				await _dispatcher.PostAsync(() => HandleNewPayload(hash, () => new ClipboardFilesEntry
				{
					FileSystemEntries = fileSystemEntries,
					Hash = hash
				}, isSensitive)).ConfigureAwait(false);

				return;
			}

			if (await TryReadImagePngAsync() is { Length: > 0 } png)
			{
				byte[] hash = ComputeHash(png);

				await _dispatcher.PostAsync(() => HandleNewPayload(hash, () => new ClipboardImageEntry
				{
					OriginalPng = png,
					Hash = hash
				}, isSensitive)).ConfigureAwait(false);

				return;
			}

			if (await TryReadTextAsync() is { Length: > 0 } text)
			{
				// Read companion formats up front so the hash reflects them: the same text copied as
				// plain (Notepad) vs formatted (Word) is genuinely different and must not be deduped.
				string? html = await TryReadFormattedTextAsync(HtmlFormat).ConfigureAwait(false);

				string? rtf = await TryReadFormattedTextAsync(RtfFormat).ConfigureAwait(false);

				byte[] hash = ComputeTextEntryHash(text, html, rtf);

				await _dispatcher
					.PostAsync(() => HandleNewPayload(hash, () => BuildTextEntry(text, html, rtf, hash), isSensitive))
					.ConfigureAwait(false);

				return;
			}

			// Nothing capturable on the clipboard (e.g. emptied via Win+V "Clear all") — drop the stale highlight.
			await _dispatcher
				.PostAsync(HandleClipboardCleared)
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
	/// Attaches a formatted-text payload (HTML / RTF) to <paramref name="item" />.
	/// </summary>
	private static void AttachFormattedText(
		DataTransferItem item,
		string payload,
		DataFormat<byte[]>? format)
	{
		if (format is null)
		{
			return;
		}

		item.Set(format, TextHelper.Utf8Encoding.GetBytes(payload));
	}

	/// <summary>
	/// Builds the <c>x-special/gnome-copied-files</c> payload: "copy" then one file:// URI per line.
	/// </summary>
	private static string BuildGnomeCopiedFiles(IEnumerable<IStorageItem> items)
	{
		return string.Join('\n', items.Select(static item => item.Path.AbsoluteUri).Prepend("copy"));
	}

	/// <summary>
	/// Computes SHA-256 of <paramref name="data" />.
	/// </summary>
	private static byte[] ComputeHash(ReadOnlySpan<byte> data) => SHA256.HashData(data);

	/// <summary>
	/// Builds the Linux GNOME copied-files platform format; <c>null</c> on non-Linux platforms.
	/// </summary>
	private static DataFormat<byte[]>? GetGnomeCopiedFilesFormat()
	{
		return AppUtils.IsLinux
			? DataFormat.CreateBytesPlatformFormat("x-special/gnome-copied-files")
			: null;
	}

	/// <summary>
	/// Builds the platform-specific HTML byte[] format.
	/// </summary>
	private static DataFormat<byte[]>? GetHtmlFormat()
	{
		if (AppUtils.IsWindows)
		{
			return DataFormat.CreateBytesPlatformFormat("HTML Format");
		}
		else if (AppUtils.IsMacOs)
		{
			return DataFormat.CreateBytesPlatformFormat("public.html");
		}
		else
		{
			return DataFormat.CreateBytesPlatformFormat("text/html");
		}
	}

	/// <summary>
	/// Builds the platform-specific RTF byte[] format.
	/// </summary>
	private static DataFormat<byte[]>? GetRtfFormat()
	{
		if (AppUtils.IsWindows)
		{
			return DataFormat.CreateBytesPlatformFormat("Rich Text Format");
		}
		else if (AppUtils.IsMacOs)
		{
			return DataFormat.CreateBytesPlatformFormat("public.rtf");
		}
		else
		{
			return DataFormat.CreateBytesPlatformFormat("text/rtf");
		}
	}

	/// <summary>
	/// <c>True</c> when two payload hashes are byte-equal.
	/// </summary>
	private static bool HashEquals(byte[] left, byte[] right) => left.AsSpan().SequenceEqual(right);

	/// <summary>
	/// Returns the trimmed value of <paramref name="text" /> when it matches an
	/// absolute http(s) URL (whole-string match); otherwise <c>null</c>.
	/// </summary>
	private static string? TryDetectUrl(string text)
	{
		string trimmed = text.Trim();

		return WholeStringUrlRegex.IsMatch(trimmed) ? trimmed : null;
	}

	/// <summary>
	/// Clears the active-entry highlight from every entry. UI-thread only.
	/// </summary>
	private void ClearActive()
	{
		if (Entries.FirstOrDefault(static entry => entry.IsActive) is not { } active)
		{
			return;
		}

		active.IsActive = false;
	}

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

				if (preservePinned)
				{
					for (int i = Entries.Count - 1; i >= 0; i--)
					{
						if (!Entries[i].IsPinned)
						{
							Entries.RemoveAt(i);
						}
					}
				}
				else
				{
					Entries.Clear();
				}

				remaining = Entries.Count;

				_lastHash = null;

				ClearActive();
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
			_logger.LogException(ex, assertDebug: false);
		}
		finally
		{
			_clearGate.Release();
		}
	}

	/// <summary>
	/// <c>True</c> when the clipboard advertises any sensitivity marker format,
	/// i.e. a password manager flagged the copy.
	/// </summary>
	private async Task<bool> ContainsSensitivityMarkerAsync()
	{
		try
		{
			IReadOnlyList<DataFormat> formats = await _clipboard
				.GetDataFormatsAsync()
				.ConfigureAwait(false);

			if (formats.Any(static format => format.Identifier == ClipboardSensitivityMarkers.ClipboardHistoryItemId))
			{
				return false;
			}

			bool hasHistoryFlag = false;

			foreach (DataFormat format in formats)
			{
				if (SensitivityMarkerIdentifiers.Contains(format.Identifier))
				{
					return true;
				}

				if (format.Identifier == ClipboardSensitivityMarkers.CanIncludeInClipboardHistory)
				{
					hasHistoryFlag = true;
				}
			}

			return hasHistoryFlag && await IsHistoryExcludedAsync().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return false;
		}
	}

	/// <summary>
	/// Returns the number of pinned entries, which by invariant form a contiguous block atop <see cref="Entries" />;
	/// also the index of the first unpinned entry. UI-thread only.
	/// </summary>
	private int GetPinnedCount() => Entries.Count(x => x.IsPinned);

	/// <summary>
	/// Drops the active highlight and change-detection baseline when the clipboard holds nothing capturable
	/// (e.g. emptied via Win+V); a restore awaiting its echo is left untouched. UI-thread only.
	/// </summary>
	private void HandleClipboardCleared()
	{
		if (_restoredEntry is not null)
		{
			return;
		}

		_lastHash = null;

		ClearActive();
	}

	/// <summary>
	/// Inserts <paramref name="entry" /> below the pinned block and enforces the log cap on
	/// unpinned entries only (pinned ones are exempt from trimming).
	/// </summary>
	private void InsertAtTop(ClipboardLogEntryBase entry)
	{
		Entries.Insert(GetPinnedCount(), entry);

		while (Entries.Count - GetPinnedCount() > LogLimit)
		{
			// The last entry is always unpinned (pinned ones sit atop), so trimming never drops a pin.
			Entries.RemoveAt(Entries.Count - 1);
		}

		MarkActive(entry);

		NotifyChanged(ClipboardLogChangeKind.Updated);

		NotifyEntryCountChanged();
	}

	/// <summary>
	/// Moves an existing entry with the same <paramref name="hash" /> to the top, otherwise inserts a new one.
	/// </summary>
	private void InsertOrMoveToTop(byte[] hash, Func<ClipboardLogEntryBase> entryFactory)
	{
		if (Entries.FirstOrDefault(x => HashEquals(x.Hash, hash)) is { } existing)
		{
			_logger.LogDebug($"Moving existing clipboard entry to top: {existing.GetType().Name}.");

			MoveToTop(existing);

			return;
		}

		ClipboardLogEntryBase entry = entryFactory();

		_logger.LogDebug($"Captured new clipboard entry: {entry.GetType().Name}.");

		InsertAtTop(entry);
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
	/// <c>True</c> when <paramref name="hash" /> is a self-echo (from our own restore) or
	/// matches the last observed payload.
	/// </summary>
	private bool IsEchoOrUnchanged(byte[] hash)
	{
		if (_lastHash is { } prev && HashEquals(prev, hash))
		{
			return true;
		}

		_lastHash = hash;

		return false;
	}

	/// <summary>
	/// <c>True</c> when the <see cref="ClipboardSensitivityMarkers.CanIncludeInClipboardHistory" /> DWORD reads as 0 (exclude from log).
	/// </summary>
	private async Task<bool> IsHistoryExcludedAsync()
	{
		DataFormat<byte[]> format = DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory);

		byte[]? value = await _clipboard
			.TryGetValueAsync(format)
			.ConfigureAwait(false);

		// A missing or non-zero value means the content is allowed in log (not sensitive).
		return value is { Length: > 0 } && value.All(static x => x == 0);
	}

	/// <summary>
	/// Polling loop.
	/// </summary>
	private async Task LoopAsync(CancellationTokenSource cancellation)
	{
		CancellationToken token = cancellation.Token;

		TimeSpan pollInterval = TimeSpan.FromMilliseconds(750.0);

		_logger.LogInformation(
			$"{nameof(ClipboardLogService)} loop started (interval = {pollInterval.TotalMilliseconds:F0} ms, limit = {LogLimit}).");

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
	/// Marks <paramref name="entry" /> (already in <see cref="Entries" />) as the active
	/// current-clipboard entry, clearing the flag on all others. UI-thread only.
	/// </summary>
	private void MarkActive(ClipboardLogEntryBase entry)
	{
		if (Entries.FirstOrDefault(static other => other.IsActive) is { } previous && !ReferenceEquals(previous, entry))
		{
			previous.IsActive = false;
		}

		entry.IsActive = true;
	}

	/// <summary>
	/// Moves <paramref name="entry" /> to the top of its block.
	/// </summary>
	private void MoveToTop(ClipboardLogEntryBase entry)
	{
		try
		{
			int index = Entries.IndexOf(entry);

			if (index < 0)
			{
				InsertAtTop(entry);

				return;
			}

			MarkActive(entry);

			// Pinned entries go to the very top; unpinned ones stay below the pinned block.
			int target = entry.IsPinned ? 0 : GetPinnedCount();

			if (index == target)
			{
				return;
			}

			Entries.Move(index, target);

			NotifyChanged(ClipboardLogChangeKind.Updated);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
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

	/// <summary>
	/// Replaces the just-restored <paramref name="restored" /> entry with <paramref name="rebaselined" />
	/// (the content as the clipboard handed it back), so its persisted hash matches what a future
	/// session will capture for the same content. UI-thread only.
	/// </summary>
	private void RebaselineRestored(ClipboardLogEntryBase restored, ClipboardLogEntryBase rebaselined)
	{
		rebaselined.IsPinned = restored.IsPinned;

		int index = Entries.IndexOf(restored);

		if (index < 0)
		{
			InsertAtTop(rebaselined);

			return;
		}

		Entries[index] = rebaselined;

		MarkActive(rebaselined);

		_logger.LogDebug($"Re-baselined restored clipboard entry: {rebaselined.GetType().Name}.");

		NotifyChanged(ClipboardLogChangeKind.Updated);
	}

	/// <summary>
	/// Resolves stored paths back into <see cref="IStorageItem" /> instances via the
	/// application's <see cref="IStorageProvider" /> and pushes them onto the clipboard.
	/// Missing items are silently dropped (best-effort restore).
	/// </summary>
	private async Task SetFilesAsync(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		try
		{
			List<IStorageItem> resolved = new(entries.Count);

			foreach (ClipboardFileSystemEntry entry in entries)
			{
				if (string.IsNullOrWhiteSpace(entry.Path))
				{
					continue;
				}

				try
				{
					IStorageItem? item = entry.IsFolder
						? await _storage.TryGetFolderFromPathAsync(entry.Path).ConfigureAwait(false)
						: await _storage.TryGetFileFromPathAsync(entry.Path).ConfigureAwait(false);

					if (item is not null)
					{
						resolved.Add(item);
					}
				}
				catch (Exception ex)
				{
					_logger.LogException(ex, assertDebug: false);
				}
			}

			if (resolved.Count == 0)
			{
				return;
			}

			// Do NOT dispose: ownership of the DataTransfer passes to the clipboard (delayed rendering).
			DataTransfer transfer = new();

			foreach (IStorageItem item in resolved)
			{
				transfer.Add(DataTransferItem.CreateFile(item));
			}

			// On Linux file managers paste files from x-special/gnome-copied-files, not from the
			// text/uri-list that Avalonia advertises — add it explicitly so Ctrl+V works there.
			if (GnomeCopiedFilesFormat.Value is { } gnomeFormat)
			{
				DataTransferItem gnomeItem = new();

				gnomeItem.Set(gnomeFormat, TextHelper.Utf8Encoding.GetBytes(BuildGnomeCopiedFiles(resolved)));

				transfer.Add(gnomeItem);
			}

			await _clipboard
				.SetDataAsync(transfer)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}

	/// <summary>
	/// Decodes <paramref name="png" /> bytes into an Avalonia <see cref="Bitmap" />
	/// and pushes it back to the system clipboard.
	/// </summary>
	private async Task SetImageAsync(byte[] png)
	{
		try
		{
			await using MemoryStream stream = new(png);

			// Do NOT dispose: ownership passes to the clipboard (delayed rendering) — disposing
			// here yields an empty paste. GC reclaims it once clipboard ownership changes.
			Bitmap bitmap = new(stream);

			await _clipboard
				.SetBitmapAsync(bitmap)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}

	/// <summary>
	/// Writes <paramref name="text" /> together with optional <paramref name="html" />
	/// and <paramref name="rtf" /> payloads as a single <see cref="DataTransfer" />,
	/// so paste targets pick whichever format they support.
	/// </summary>
	private async Task SetTextWithFormatsAsync(
		string text,
		string? html,
		string? rtf,
		bool isSensitive)
	{
		try
		{
			DataTransferItem item = new();

			item.SetText(text);

			if (html is not null)
			{
				AttachFormattedText(item, html, HtmlFormat);
			}

			if (rtf is not null)
			{
				AttachFormattedText(item, rtf, RtfFormat);
			}

			if (isSensitive)
			{
				ClipboardSensitivityMarkerWriter.AttachSensitivityMarkers(item);
			}

			// Do NOT dispose: ownership of the DataTransfer passes to the clipboard (delayed rendering).
			DataTransfer transfer = new();

			transfer.Add(item);

			await _clipboard
				.SetDataAsync(transfer)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);
		}
	}

	/// <summary>
	/// Reads file/folder items from the clipboard, if any, and projects them into
	/// <see cref="ClipboardFileSystemEntry" /> records. Items without a local path are skipped.
	/// </summary>
	private async Task<IReadOnlyList<ClipboardFileSystemEntry>?> TryReadFilesAsync()
	{
		IStorageItem[]? items;

		try
		{
			items = await _clipboard
				.TryGetFilesAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return null;
		}

		if (items is null || items.Length == 0)
		{
			return null;
		}

		List<ClipboardFileSystemEntry> result = new(items.Length);

		foreach (IStorageItem item in items)
		{
			if (item.Path is not { IsAbsoluteUri: true } uri)
			{
				continue;
			}

			string path = uri.LocalPath;

			if (string.IsNullOrEmpty(path))
			{
				continue;
			}

			result.Add(new ClipboardFileSystemEntry(path, IsFolder: item is IStorageFolder));
		}

		if (result.Count == 0)
		{
			return null;
		}

		// Stable order: folders first, then files; alphabetical within each group.
		result.Sort(static (x, y) =>
		{
			int byKind = y.IsFolder.CompareTo(x.IsFolder);

			return byKind != 0
				? byKind
				: string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		});

		return result;
	}

	/// <summary>
	/// Reads a formatted-text payload (HTML / RTF).
	/// </summary>
	private async Task<string?> TryReadFormattedTextAsync(DataFormat<byte[]>? format)
	{
		if (format is null)
		{
			return null;
		}

		try
		{
			byte[]? bytes = await _clipboard
				.TryGetValueAsync(format)
				.ConfigureAwait(false);

			return bytes is null
				? null
				: TextHelper.Utf8Encoding.GetString(bytes);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return null;
		}
	}

	/// <summary>
	/// Reads a clipboard bitmap (if any) and re-encodes it to PNG bytes.
	/// </summary>
	private async Task<byte[]?> TryReadImagePngAsync()
	{
		Bitmap? bitmap;

		try
		{
			bitmap = await _clipboard
				.TryGetBitmapAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return null;
		}

		if (bitmap is null)
		{
			return null;
		}

		try
		{
			await using MemoryStream output = new();

			bitmap.Save(output);

			return output.ToArray();
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return null;
		}
		finally
		{
			bitmap.Dispose();
		}
	}

	/// <summary>
	/// Reads clipboard text if available.
	/// </summary>
	private async Task<string?> TryReadTextAsync()
	{
		try
		{
			return await _clipboard
				.TryGetTextAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, assertDebug: false);

			return null;
		}
	}
	#endregion
}
