using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Cysharp.Text;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Common;
using Shared.Extensions;
using System;
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
	private static readonly DataFormat<byte[]>? GnomeCopiedFilesFormat = GetGnomeCopiedFilesFormat();

	/// <summary>
	/// Platform byte[] format for HTML ("HTML Format" / "public.html" / "text/html"), UTF-8 managed by us.
	/// byte[] — not string — so the custom MIME round-trips on the X11 clipboard when served to other apps.
	/// </summary>
	private static readonly DataFormat<byte[]>? HtmlFormat = GetHtmlFormat();

	/// <summary>
	/// Interval between clipboard polls.
	/// </summary>
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(750.0);

	/// <summary>
	/// Platform byte[] format for RTF ("Rich Text Format" / "public.rtf" / "text/rtf"). See <see cref="HtmlFormat" />.
	/// </summary>
	private static readonly DataFormat<byte[]>? RtfFormat = GetRtfFormat();

	/// <summary>
	/// Matches only when the entire trimmed text is an http(s) URL (used to pick
	/// <see cref="ClipboardUrlEntry" /> over <see cref="ClipboardTextEntry" />).
	/// </summary>
	private static readonly Regex WholeStringUrlRegex = GetWholeStringUrlRegex();

	/// <inheritdoc cref="Application" />
	private readonly Application _app;

	/// <summary>
	/// Serializes <see cref="PollOnceAsync" /> against <see cref="ClearAsync" /> so a poll
	/// tick cannot insert an entry while the history / clipboard is being cleared.
	/// </summary>
	private readonly SemaphoreSlim _clearGate = new(1, 1);

	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;

	/// <inheritdoc cref="ITaskExceptionHandler" />
	private readonly ITaskExceptionHandler _exceptionHandler;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Guards <see cref="StartAsync" /> against double-start. Loop sets this to
	/// <c>False</c> in its finally block once it exits.
	/// </summary>
	private bool _isLoopRunning;

	/// <summary>
	/// Hash of the most recently observed clipboard payload.
	/// </summary>
	private byte[]? _lastHash;

	/// <summary>
	/// Linked cancellation source created in <see cref="StartAsync" /> from the user token.
	/// </summary>
	private CancellationTokenSource? _stopCts;

	/// <summary>
	/// When <c>True</c>, the next <see cref="PollOnceAsync" /> tick skips its match check.
	/// </summary>
	private bool _suppressEcho;
	#endregion

	#region Constructors
	public ClipboardHistoryService(
		Application app,
		IDispatcher dispatcher,
		ILogger logger,
		ITaskExceptionHandler exceptionHandler)
	{
		_app = app;

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
			.ConfigureAwait(true);

		try
		{
			Entries.Clear();

			_lastHash = null;

			// Emptying the OS clipboard too, so the just-cleared content is not re-captured
			// by the next poll tick (it only reappears on a genuine new copy).
			if (_app.FindClipboard() is not { } clipboard)
			{
				return;
			}

			await clipboard
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

		await _clearGate
			.WaitAsync()
			.ConfigureAwait(false);

		_clearGate.Dispose();
	}

	/// <inheritdoc />
	public async Task RestoreAsync(ClipboardHistoryEntryBase entry)
	{
		if (Volatile.Read(ref _isDisposed))
		{
			return;
		}

		if (_app.FindClipboard() is not { } clipboard)
		{
			return;
		}

		// Mark next poll tick as a self-echo so we don't insert a duplicate entry.
		Interlocked.Exchange(ref _suppressEcho, true);

		_lastHash = entry.Hash;

		try
		{
			switch (entry)
			{
				case ClipboardTextEntry textEntry:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardTextEntry)}, {textEntry.Text.Length} chars" +
						$"{(textEntry.IsHtml ? " + HTML" : string.Empty)}" +
						$"{(textEntry.IsRtf ? " + RTF" : string.Empty)}.");

					await DispatchWatchedAsync(() => SetTextWithFormatsAsync(
						clipboard,
						textEntry.Text,
						textEntry.Html,
						textEntry.Rtf)).ConfigureAwait(false);
					break;

				case ClipboardImageEntry imageEntry when imageEntry.OriginalPng is { Length: > 0 } png:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardImageEntry)}, {png.Length} bytes (PNG).");

					await DispatchWatchedAsync(() => SetImageAsync(clipboard, png)).ConfigureAwait(false);
					break;

				case ClipboardFilesEntry filesEntry when filesEntry.FileSystemEntries is { Count: > 0 } fileSystemEntries:
					_logger.LogInformation(
						$"Restoring clipboard entry: {nameof(ClipboardFilesEntry)}, {fileSystemEntries.Count} items.");

					await DispatchWatchedAsync(() => SetFilesAsync(clipboard, fileSystemEntries)).ConfigureAwait(false);
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
		if (Volatile.Read(ref _isDisposed))
		{
			return Task.CompletedTask;
		}

		CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

		Interlocked
			.Exchange(ref _stopCts, cancellation)?
			.Dispose();

		// Idempotent: ignore a second StartAsync while the loop is alive.
		if (Interlocked.Exchange(ref _isLoopRunning, true))
		{
			return Task.CompletedTask;
		}

		return LoopAsync(cancellation.Token);
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
	/// Builds the <c>x-special/gnome-copied-files</c> payload: "copy" then one file:// URI per line.
	/// </summary>
	private static string BuildGnomeCopiedFiles(IEnumerable<IStorageItem> items)
	{
		return string.Join('\n', items.Select(static item => item.Path.AbsoluteUri).Prepend("copy"));
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
	/// Returns value for <see cref="GnomeCopiedFilesFormat" />.
	/// </summary>
	private static DataFormat<byte[]>? GetGnomeCopiedFilesFormat()
	{
		return AppUtils.IsLinux
			? DataFormat.CreateBytesPlatformFormat("x-special/gnome-copied-files")
			: null;
	}

	/// <summary>
	/// Returns value for <see cref="HtmlFormat" />.
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
	/// Returns value for <see cref="RtfFormat" />.
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
	/// Returns value for <see cref="WholeStringUrlRegex" />: matches when the entire input is an http(s) URL.
	/// </summary>
	[GeneratedRegex(@"^https?://\S+$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex GetWholeStringUrlRegex();

	/// <summary>
	/// Returns <c>True</c> when two payload hashes are byte-equal.
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
	/// Writes <paramref name="text" /> together with optional <paramref name="html" />
	/// and <paramref name="rtf" /> payloads as a single <see cref="DataTransfer" />,
	/// so paste targets pick whichever format they support.
	/// </summary>
	private static Task SetTextWithFormatsAsync(
		IClipboard clipboard,
		string text,
		string? html,
		string? rtf)
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

		DataTransfer transfer = new();

		transfer.Add(item);

		return clipboard.SetDataAsync(transfer);
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
	/// Posts <paramref name="operation" /> to the UI thread, wrapped by the exception handler.
	/// </summary>
	private Task DispatchWatchedAsync(Func<Task> operation)
	{
		return _dispatcher.PostAsync(() => _exceptionHandler.Watch(operation()));
	}

	/// <summary>
	/// Compares <paramref name="hash" /> with <see cref="_lastHash" /> and pushes a new
	/// entry to the top of the list when it changed.
	/// </summary>
	private void HandleNewPayload(byte[] hash, Func<ClipboardHistoryEntryBase> entryFactory)
	{
		if (Interlocked.Exchange(ref _suppressEcho, false))
		{
			_lastHash = hash;

			return;
		}

		if (_lastHash is { } prev && HashEquals(prev, hash))
		{
			return;
		}

		_lastHash = hash;

		// If the same payload is somewhere down the list — move it up, don't duplicate.
		if (Entries.FirstOrDefault(x => HashEquals(x.Hash, hash)) is { } existing)
		{
			MoveToTop(existing);

			return;
		}

		InsertAtTop(entryFactory());
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
	/// Polling loop.
	/// </summary>
	private async Task LoopAsync(CancellationToken token)
	{
		const ConfigureAwaitOptions awaitOptions = ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext;

		_logger.LogInformation(
			$"{nameof(ClipboardHistoryService)} loop started (interval = {PollInterval.TotalMilliseconds:F0} ms, limit = {HistoryLimit}).");

		try
		{
			while (!Volatile.Read(ref _isDisposed) && !token.IsCancellationRequested)
			{
				await Task
					.Delay(PollInterval, token)
					.ConfigureAwait(awaitOptions);

				if (Volatile.Read(ref _isDisposed) || token.IsCancellationRequested)
				{
					break;
				}

				await PollOnceAsync().ConfigureAwait(true);
			}
		}
		finally
		{
			Interlocked.Exchange(ref _isLoopRunning, false);

			Interlocked
				.Exchange(ref _stopCts, null)?
				.Dispose();

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
			.ConfigureAwait(true);

		try
		{
			if (_app.FindClipboard() is not { } clipboard)
			{
				return;
			}

			if (await TryReadFilesAsync(clipboard) is { Count: > 0 } fileSystemEntries)
			{
				byte[] hash = HashFiles(fileSystemEntries);

				HandleNewPayload(hash, () => new ClipboardFilesEntry
				{
					FileSystemEntries = fileSystemEntries,
					Hash = hash
				});

				return;
			}

			if (await TryReadImagePngAsync(clipboard) is { Length: > 0 } png)
			{
				byte[] hash = ComputeHash(png);

				HandleNewPayload(hash, () => new ClipboardImageEntry
				{
					OriginalPng = png,
					Hash = hash
				});

				return;
			}

			if (await TryReadTextAsync(clipboard) is { Length: > 0 } text)
			{
				byte[] hash = ComputeHash(TextHelper.Utf8Encoding.GetBytes(text));

				string? html = await TryReadHtmlAsync(clipboard).ConfigureAwait(false);

				string? rtf = await TryReadRtfAsync(clipboard).ConfigureAwait(false);

				HandleNewPayload(hash, () => BuildTextEntry(text, html, rtf, hash));
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
	private async Task SetFilesAsync(IClipboard clipboard, IReadOnlyList<ClipboardFileSystemEntry> entries)
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
		if (GnomeCopiedFilesFormat is { } gnomeFormat)
		{
			DataTransferItem gnomeItem = new();

			gnomeItem.Set(gnomeFormat, TextHelper.Utf8Encoding.GetBytes(BuildGnomeCopiedFiles(resolved)));

			transfer.Add(gnomeItem);
		}

		await clipboard
			.SetDataAsync(transfer)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Reads file/folder items from the clipboard, if any, and projects them into
	/// <see cref="ClipboardFileSystemEntry" /> records. Items without a local path are skipped.
	/// </summary>
	private async Task<IReadOnlyList<ClipboardFileSystemEntry>?> TryReadFilesAsync(IClipboard clipboard)
	{
		IStorageItem[]? items;

		try
		{
			items = await clipboard
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
	private async Task<string?> TryReadFormattedTextAsync(
		IClipboard clipboard,
		DataFormat<byte[]>? format)
	{
		if (format is null)
		{
			return null;
		}

		try
		{
			byte[]? bytes = await clipboard
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

	/// <summary>Reads the HTML payload from the clipboard.</summary>
	private Task<string?> TryReadHtmlAsync(IClipboard clipboard) => TryReadFormattedTextAsync(clipboard, HtmlFormat);

	/// <summary>
	/// Reads a clipboard bitmap (if any) and re-encodes it to PNG bytes.
	/// </summary>
	private async Task<byte[]?> TryReadImagePngAsync(IClipboard clipboard)
	{
		Bitmap? bitmap;

		try
		{
			bitmap = await clipboard
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

	/// <summary>Reads the RTF payload from the clipboard.</summary>
	private Task<string?> TryReadRtfAsync(IClipboard clipboard) => TryReadFormattedTextAsync(clipboard, RtfFormat);

	/// <summary>
	/// Reads clipboard text if available.
	/// </summary>
	private async Task<string?> TryReadTextAsync(IClipboard clipboard)
	{
		try
		{
			return await clipboard
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
