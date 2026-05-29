using Avalonia;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class ClipboardHistoryService : IClipboardHistoryService, IDisposable
{
	#region Properties
	/// <inheritdoc />
	public ObservableCollection<ClipboardHistoryEntry> Entries { get; } = [];

	/// <inheritdoc />
	public bool IsRunning => Volatile.Read(ref _isLoopRunning);
	#endregion

	#region Data
	/// <inheritdoc cref="Application" />
	private readonly Application _app;

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
	/// <c>false</c> in its finally block once it exits.
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
	/// When non-zero, the next <see cref="PollOnceAsync" /> tick skips its match check.
	/// </summary>
	private int _suppressEcho;
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
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		Stop();
	}

	/// <inheritdoc />
	public async Task RestoreAsync(ClipboardHistoryEntry entry)
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
		Interlocked.Exchange(ref _suppressEcho, 1);

		_lastHash = entry.Hash;

		try
		{
			switch (entry.Kind)
			{
				case ClipboardEntryKind.Text when entry.Text is { } text:
					_logger.LogInformation(
						$"Restoring clipboard entry: {ClipboardEntryKind.Text}, {text.Length} chars.");

					await _dispatcher
						.PostAsync(() => _exceptionHandler.Watch(clipboard.SetTextAsync(text)))
						.ConfigureAwait(false);
					break;

				case ClipboardEntryKind.Image when entry.OriginalPng is { Length: > 0 } png:
					_logger.LogInformation(
						$"Restoring clipboard entry: {ClipboardEntryKind.Image}, {png.Length} bytes (PNG).");

					await _dispatcher
						.PostAsync(() => _exceptionHandler.Watch(SetImageAsync(clipboard, png)))
						.ConfigureAwait(false);
					break;

				case ClipboardEntryKind.Files when entry.FileEntries is { Count: > 0 } fileEntries:
					_logger.LogInformation(
						$"Restoring clipboard entry: {ClipboardEntryKind.Files}, {fileEntries.Count} items.");

					await _dispatcher
						.PostAsync(() => _exceptionHandler.Watch(SetFilesAsync(clipboard, fileEntries)))
						.ConfigureAwait(false);
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
	/// Computes SHA-256 of <paramref name="data" />.
	/// </summary>
	private static byte[] ComputeHash(ReadOnlySpan<byte> data) => SHA256.HashData(data);

	/// <summary>
	/// Hashes the file list, including kind marker so a folder and a file with the same
	/// path don't collide. Order-sensitive — selection order is preserved by the OS.
	/// </summary>
	private static byte[] HashFiles(IReadOnlyList<ClipboardFileEntry> entries)
	{
		StringBuilder sb = new();

		foreach (ClipboardFileEntry entry in entries)
		{
			sb.Append(entry.IsFolder ? 'D' : 'F');

			sb.Append('|');

			sb.Append(entry.Path);

			sb.Append('\0');
		}

		return ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
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
	/// Compares <paramref name="hash" /> with <see cref="_lastHash" /> and pushes a new
	/// entry to the top of the list when it changed.
	/// </summary>
	private void HandleNewPayload(byte[] hash, Func<ClipboardHistoryEntry> entryFactory)
	{
		if (Interlocked.Exchange(ref _suppressEcho, 0) == 1)
		{
			_lastHash = hash;

			return;
		}

		if (_lastHash is { } prev && prev.AsSpan().SequenceEqual(hash))
		{
			return;
		}

		_lastHash = hash;

		// If the same payload is somewhere down the list — move it up, don't duplicate.
		if (Entries.FirstOrDefault(x => x.Hash.AsSpan().SequenceEqual(hash)) is { } existing)
		{
			MoveToTop(existing);

			return;
		}

		InsertAtTop(entryFactory());
	}

	/// <summary>
	/// Inserts <paramref name="entry" /> at position 0 and enforces the history cap.
	/// </summary>
	private void InsertAtTop(ClipboardHistoryEntry entry)
	{
		Entries.Insert(0, entry);

		while (Entries.Count > IClipboardHistoryService.HistoryLimit)
		{
			Entries.RemoveAt(Entries.Count - 1);
		}
	}

	/// <summary>
	/// Polling loop.
	/// </summary>
	private async Task LoopAsync(CancellationToken token)
	{
		TimeSpan interval = TimeSpan.FromMilliseconds(750.0);

		const ConfigureAwaitOptions awaitOptions = ConfigureAwaitOptions.SuppressThrowing | ConfigureAwaitOptions.ContinueOnCapturedContext;

		_logger.LogInformation(
			$"{nameof(ClipboardHistoryService)} loop started (interval = {interval.TotalMilliseconds:F0} ms, limit = {IClipboardHistoryService.HistoryLimit}).");

		try
		{
			while (!Volatile.Read(ref _isDisposed) && !token.IsCancellationRequested)
			{
				await Task
					.Delay(interval, token)
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
	private void MoveToTop(ClipboardHistoryEntry entry)
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
		try
		{
			if (_app.FindClipboard() is not { } clipboard)
			{
				return;
			}

			if (await TryReadFilesAsync(clipboard) is { Count: > 0 } fileEntries)
			{
				byte[] hash = HashFiles(fileEntries);

				HandleNewPayload(hash, () => new ClipboardHistoryEntry
				{
					Kind = ClipboardEntryKind.Files,
					FileEntries = fileEntries,
					Hash = hash,
					Timestamp = DateTimeOffset.Now
				});

				return;
			}

			if (await TryReadImagePngAsync(clipboard) is { Length: > 0 } png)
			{
				byte[] hash = ComputeHash(png);

				HandleNewPayload(hash, () => new ClipboardHistoryEntry
				{
					Kind = ClipboardEntryKind.Image,
					OriginalPng = png,
					Hash = hash,
					Timestamp = DateTimeOffset.Now
				});

				return;
			}

			if (await TryReadTextAsync(clipboard) is { Length: > 0 } text)
			{
				byte[] hash = ComputeHash(Encoding.UTF8.GetBytes(text));

				HandleNewPayload(hash, () => new ClipboardHistoryEntry
				{
					Kind = ClipboardEntryKind.Text,
					Text = text,
					Hash = hash,
					Timestamp = DateTimeOffset.Now
				});
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug($"Clipboard poll failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Resolves stored paths back into <see cref="IStorageItem" /> instances via the
	/// application's <see cref="IStorageProvider" /> and pushes them onto the clipboard.
	/// Missing items are silently dropped (best-effort restore).
	/// </summary>
	private async Task SetFilesAsync(IClipboard clipboard, IReadOnlyList<ClipboardFileEntry> entries)
	{
		if (_app.FindStorageProvider() is not { } provider)
		{
			return;
		}

		List<IStorageItem> resolved = new(entries.Count);

		foreach (ClipboardFileEntry entry in entries)
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

		await clipboard
			.SetDataAsync(transfer)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Reads file/folder items from the clipboard, if any, and projects them into
	/// <see cref="ClipboardFileEntry" /> records. Items without a local path are skipped.
	/// </summary>
	private async Task<IReadOnlyList<ClipboardFileEntry>?> TryReadFilesAsync(IClipboard clipboard)
	{
		IReadOnlyList<IStorageItem>? items;

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

		if (items is null || items.Count == 0)
		{
			return null;
		}

		List<ClipboardFileEntry> result = new(items.Count);

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

			result.Add(new ClipboardFileEntry(path, IsFolder: item is IStorageFolder));
		}

		return result.Count == 0 ? null : result;
	}

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
