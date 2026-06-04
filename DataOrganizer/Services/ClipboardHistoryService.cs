using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Cysharp.Text;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
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

namespace DataOrganizer.Services;

public sealed partial class ClipboardHistoryService : IClipboardHistoryService
{
	#region Properties
	/// <inheritdoc />
	public ObservableCollection<ClipboardHistoryEntryBase> Entries { get; } = [];

	/// <inheritdoc />
	public bool IsRunning => Volatile.Read(ref _isLoopRunning);
	#endregion

	#region Data
	/// <summary>
	/// Maximum number of entries kept in history. Matches the Windows
	/// system clipboard (Win+V) limit of 25 records.
	/// </summary>
	private const int HistoryLimit = 25;

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
		ClipboardSensitivityMarkers.CanIncludeInClipboardHistory,
		ClipboardSensitivityMarkers.ClipboardViewerIgnore,
		ClipboardSensitivityMarkers.KdePasswordManagerHint,
		ClipboardSensitivityMarkers.NsPasteboardConcealedType
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Sensitivity marker formats written back to the clipboard when restoring a sensitive entry,
	/// so other clipboard managers / Win+V skip it. Platform-specific; empty on unsupported platforms.
	/// </summary>
	private static readonly (DataFormat<byte[]> Format, byte[] Value)[] SensitivityMarkersToWrite = BuildSensitivityMarkersToWrite();

	/// <summary>
	/// Matches only when the entire trimmed text is an http(s) URL (used to pick
	/// <see cref="ClipboardUrlEntry" /> over <see cref="ClipboardTextEntry" />).
	/// </summary>
	private static readonly Regex WholeStringUrlRegex = GetWholeStringUrlRegex();

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <summary>
	/// Serializes a poll tick against a clear operation so a poll tick cannot
	/// insert an entry while the history / clipboard is being cleared.
	/// </summary>
	private readonly SemaphoreSlim _clearGate = new(1, 1);

	/// <inheritdoc cref="IClipboardAccessor" />
	private readonly IClipboardAccessor _clipboard;

	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

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
	/// Linked cancellation source created on start from the user token.
	/// </summary>
	private CancellationTokenSource? _stopCts;

	/// <summary>
	/// When <c>True</c>, the next poll tick skips its match check.
	/// </summary>
	private bool _suppressEcho;
	#endregion

	#region Constructors
	public ClipboardHistoryService(
		Application app,
		IClipboardAccessor clipboard,
		IDispatcherAccessor dispatcher,
		ILogger logger,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

		_clipboard = clipboard;

		_dispatcher = dispatcher;

		_exceptionHandler = exceptionHandler;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task ClearAsync()
	{
		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			Entries.Clear();

			await _dispatcher
				.PostAsync(() => _lastHash = null)
				.ConfigureAwait(false);

			// Emptying the OS clipboard too, so the just-cleared content is not re-captured
			// by the next poll tick (it only reappears on a genuine new copy).			
			await _clipboard
				.ClearAsync()
				.ConfigureAwait(false);
		}
		finally
		{
			_clearGate.Release();
		}
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
	public async Task RestoreAsync(ClipboardHistoryEntryBase entry)
	{
		if (IsDisposed() || _app.FindClipboard() is not { } clipboard)
		{
			return;
		}

		// Mark next poll tick as a self-echo so we don't insert a duplicate entry.
		Interlocked.Exchange(ref _suppressEcho, true);

		await _dispatcher
			.PostAsync(() => _lastHash = entry.Hash)
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

					await DispatchWatchedAsync(() => SetImageAsync(clipboard, png)).ConfigureAwait(false);
					break;

				case ClipboardFilesEntry filesEntry when filesEntry.FileSystemEntries is { Count: > 0 } fileSystemEntries:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardFilesEntry)}, {fileSystemEntries.Count} items.");

					await SetFilesAsync(fileSystemEntries).ConfigureAwait(false);
					break;
			}

			// Touching Entries must happen on the UI thread.
			await _dispatcher
				.PostAsync(() => MoveToTop(entry))
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogWarning($"Restore from clipboard history failed: {ex.Message}");
		}
	}

	/// <inheritdoc />
	public Task StartAsync(CancellationToken token = default)
	{
		if (IsDisposed() || Interlocked.Exchange(ref _isLoopRunning, true))
		{
			return Task.CompletedTask;
		}

		CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

		Interlocked
			.Exchange(ref _stopCts, cancellation)?
			.Dispose();

		Task loopTask = LoopAsync(cancellation);

		_loopTask = loopTask;

		return loopTask;
	}

	/// <inheritdoc />
	public void Stop()
	{
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
	/// Attaches the sensitivity markers to <paramref name="item" /> so other clipboard managers /
	/// Win+V skip the restored content.
	/// </summary>
	private static void AttachSensitivityMarkers(DataTransferItem item)
	{
		foreach ((DataFormat<byte[]> format, byte[] value) in SensitivityMarkersToWrite)
		{
			item.Set(format, value);
		}
	}

	/// <summary>
	/// Builds the <c>x-special/gnome-copied-files</c> payload: "copy" then one file:// URI per line.
	/// </summary>
	private static string BuildGnomeCopiedFiles(IEnumerable<IStorageItem> items)
	{
		return string.Join('\n', items.Select(static item => item.Path.AbsoluteUri).Prepend("copy"));
	}

	/// <summary>
	/// Builds the platform-specific sensitivity markers written on restore.
	/// </summary>
	private static (DataFormat<byte[]> Format, byte[] Value)[] BuildSensitivityMarkersToWrite()
	{
		// Presence is enough for most markers; the Cloud Clipboard ones expect a DWORD 0.
		byte[] present = [0];

		byte[] dwordZero = [0, 0, 0, 0];

		if (AppUtils.IsWindows)
		{
			return
			[
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ExcludeFromMonitorProcessing), present),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanIncludeInClipboardHistory), dwordZero),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.CanUploadToCloudClipboard), dwordZero),
				(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.ClipboardViewerIgnore), present)
			];
		}

		if (AppUtils.IsLinux)
		{
			return [(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.KdePasswordManagerHint), TextHelper.Utf8Encoding.GetBytes("secret"))];
		}

		if (AppUtils.IsMacOs)
		{
			return [(DataFormat.CreateBytesPlatformFormat(ClipboardSensitivityMarkers.NsPasteboardConcealedType), present)];
		}

		return [];
	}
	/// <summary>
	/// Builds a text entry, choosing <see cref="ClipboardUrlEntry" /> when the text is a whole URL.
	/// </summary>
	private static ClipboardHistoryEntryBase BuildTextEntry(
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
	/// Computes SHA-256 of <paramref name="data" />.
	/// </summary>
	private static byte[] ComputeHash(ReadOnlySpan<byte> data) => SHA256.HashData(data);

	/// <summary>
	/// Hashes a text payload together with whether it carries companion formats, so the same text
	/// copied as plain (e.g. Notepad) and as formatted (e.g. Word) is treated as two distinct entries.
	/// </summary>
	private static byte[] ComputeTextEntryHash(
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
	/// Matches when the entire input is an http(s) URL.
	/// </summary>
	[GeneratedRegex(@"^https?://\S+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex GetWholeStringUrlRegex();

	/// <summary>
	/// <c>True</c> when two payload hashes are byte-equal.
	/// </summary>
	private static bool HashEquals(byte[] left, byte[] right) => left.AsSpan().SequenceEqual(right);

	/// <summary>
	/// Hashes the file list, including kind marker so a folder and a file with the same
	/// path don't collide. Order-sensitive — selection order is preserved by the OS.
	/// </summary>
	private static byte[] HashFiles(IReadOnlyList<ClipboardFileSystemEntry> entries)
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
	/// Decodes <paramref name="png" /> bytes into an Avalonia <see cref="Bitmap" />
	/// and pushes it back to the system clipboard.
	/// </summary>
	private static Task SetImageAsync(IClipboard clipboard, byte[] png)
	{
		MemoryStream stream = new(png);

		// Bitmap reads the stream eagerly during construction, but Avalonia may
		// retain a reference for the lifetime of the clipboard write. We let the
		// bitmap (and its underlying buffer) be reclaimed by GC after the write.
		Bitmap bitmap = new(stream);

		return clipboard.SetBitmapAsync(bitmap);
	}

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

			foreach (DataFormat format in formats)
			{
				if (SensitivityMarkerIdentifiers.Contains(format.Identifier))
				{
					return true;
				}
			}

			return false;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, isAssertDebug: false);

			return false;
		}
	}

	/// <summary>
	/// Posts <paramref name="operation" /> to the UI thread, wrapped by the exception handler.
	/// </summary>
	private Task DispatchWatchedAsync(Func<Task> operation)
	{
		return _dispatcher.PostAsync(() => _exceptionHandler.Watch(operation()));
	}

	/// <summary>
	/// Handles a freshly observed payload: ignores self-echoes / unchanged content, then either
	/// moves an existing matching entry to the top or inserts a new one.
	/// </summary>
	private void HandleNewPayload(byte[] hash, Func<ClipboardHistoryEntryBase> entryFactory)
	{
		if (IsEchoOrUnchanged(hash))
		{
			return;
		}

		InsertOrMoveToTop(hash, entryFactory);
	}

	/// <summary>
	/// Inserts <paramref name="entry" /> at position 0 and enforces the history cap.
	/// </summary>
	private void InsertAtTop(ClipboardHistoryEntryBase entry)
	{
		Entries.Insert(0, entry);

		while (Entries.Count > HistoryLimit)
		{
			Entries.RemoveAt(Entries.Count - 1);
		}
	}

	/// <summary>
	/// Moves an existing entry with the same <paramref name="hash" /> to the top, otherwise inserts a new one.
	/// </summary>
	private void InsertOrMoveToTop(byte[] hash, Func<ClipboardHistoryEntryBase> entryFactory)
	{
		if (Entries.FirstOrDefault(x => HashEquals(x.Hash, hash)) is { } existing)
		{
			_logger.LogDebug($"Moving existing clipboard entry to top: {existing.GetType().Name}.");

			MoveToTop(existing);

			return;
		}

		ClipboardHistoryEntryBase entry = entryFactory();

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
		if (Interlocked.Exchange(ref _suppressEcho, false))
		{
			_lastHash = hash;

			return true;
		}

		if (_lastHash is { } prev && HashEquals(prev, hash))
		{
			return true;
		}

		_lastHash = hash;

		return false;
	}

	/// <summary>
	/// Polling loop.
	/// </summary>
	private async Task LoopAsync(CancellationTokenSource cancellation)
	{
		CancellationToken token = cancellation.Token;

		TimeSpan pollInterval = TimeSpan.FromMilliseconds(750.0);

		_logger.LogInformation(
			$"{nameof(ClipboardHistoryService)} loop started (interval = {pollInterval.TotalMilliseconds:F0} ms, limit = {HistoryLimit}).");

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

			_logger.LogInformation($"{nameof(ClipboardHistoryService)} loop stopped.");
		}
	}

	/// <summary>
	/// Moves <paramref name="entry" /> to position 0 if it is already in the collection,
	/// otherwise inserts it.
	/// </summary>
	private void MoveToTop(ClipboardHistoryEntryBase entry)
	{
		int index = Entries.IndexOf(entry);

		if (index < 0)
		{
			InsertAtTop(entry);

			return;
		}

		if (index == 0)
		{
			return;
		}

		Entries.Move(index, 0);
	}

	/// <summary>
	/// Reads the clipboard once and inserts a new entry if the content changed.
	/// </summary>
	private async Task PollOnceAsync()
	{
		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		try
		{
			if (await TryReadFilesAsync() is { Count: > 0 } fileSystemEntries)
			{
				byte[] hash = HashFiles(fileSystemEntries);

				await _dispatcher.PostAsync(() => HandleNewPayload(hash, () => new ClipboardFilesEntry
				{
					FileSystemEntries = fileSystemEntries,
					Hash = hash
				})).ConfigureAwait(false);

				return;
			}

			if (await TryReadImagePngAsync() is { Length: > 0 } png)
			{
				byte[] hash = ComputeHash(png);

				await _dispatcher.PostAsync(() => HandleNewPayload(hash, () => new ClipboardImageEntry
				{
					OriginalPng = png,
					Hash = hash
				})).ConfigureAwait(false);

				return;
			}

			if (await TryReadTextAsync() is { Length: > 0 } text)
			{
				// Read companion formats up front so the hash reflects them: the same text copied as
				// plain (Notepad) vs formatted (Word) is genuinely different and must not be deduped.
				string? html = await TryReadFormattedTextAsync(HtmlFormat).ConfigureAwait(false);

				string? rtf = await TryReadFormattedTextAsync(RtfFormat).ConfigureAwait(false);

				byte[] hash = ComputeTextEntryHash(text, html, rtf);

				if (await _dispatcher.PostAsync(() => IsEchoOrUnchanged(hash)).ConfigureAwait(false))
				{
					return;
				}

				if (await ContainsSensitivityMarkerAsync().ConfigureAwait(false))
				{
					_logger.LogWarning(
						$"Skipping clipboard text entry ({text.Length} chars): a sensitivity marker was detected.");

					return;
				}

				await _dispatcher
					.PostAsync(() => InsertOrMoveToTop(hash, () => BuildTextEntry(text, html, rtf, hash)))
					.ConfigureAwait(false);
			}
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

	/// <summary>
	/// Resolves stored paths back into <see cref="IStorageItem" /> instances via the
	/// application's <see cref="IStorageProvider" /> and pushes them onto the clipboard.
	/// Missing items are silently dropped (best-effort restore).
	/// </summary>
	private async Task SetFilesAsync(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		try
		{
			if (_app.FindStorageProvider() is not { } provider)
			{
				return;
			}

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
						? await provider.TryGetFolderFromPathAsync(entry.Path).ConfigureAwait(false)
						: await provider.TryGetFileFromPathAsync(entry.Path).ConfigureAwait(false);

					if (item is not null)
					{
						resolved.Add(item);
					}
				}
				catch (Exception ex)
				{
					_logger.LogException(ex, isAssertDebug: false);
				}
			}

			if (resolved.Count == 0)
			{
				return;
			}

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
			_logger.LogException(ex, isAssertDebug: false);
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
				AttachSensitivityMarkers(item);
			}

			DataTransfer transfer = new();

			transfer.Add(item);

			await _clipboard
				.SetDataAsync(transfer)
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, isAssertDebug: false);
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
			_logger.LogException(ex, isAssertDebug: false);

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
			_logger.LogException(ex, isAssertDebug: false);

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
			_logger.LogException(ex, isAssertDebug: false);

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
			_logger.LogException(ex, isAssertDebug: false);

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
			_logger.LogException(ex, isAssertDebug: false);

			return null;
		}
	}
	#endregion
}
