using Avalonia;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using DataOrganizer.DTO;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
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
	public void Dispose() => Interlocked.Exchange(ref _isDisposed, true);

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

		// Pre-update last-seen hash to whatever we're about to write. Even if the
		// suppression flag is missed (e.g. timer fires twice fast), the hash-equality
		// check in PollOnceAsync will still skip the echo.
		_lastHash = entry.Hash;

		try
		{
			switch (entry.Kind)
			{
				case ClipboardEntryKind.Text when entry.Text is { } text:
					await _dispatcher
						.PostAsync(() => _exceptionHandler.Watch(clipboard.SetTextAsync(text)))
						.ConfigureAwait(false);
					break;

				case ClipboardEntryKind.Image when entry.OriginalPng is { Length: > 0 } png:
					await _dispatcher
						.PostAsync(() => _exceptionHandler.Watch(SetImageAsync(clipboard, png)))
						.ConfigureAwait(false);
					break;
			}

			MoveToTop(entry);
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

		// Idempotent: ignore a second StartAsync while the loop is alive.
		if (Interlocked.Exchange(ref _isLoopRunning, true))
		{
			return Task.CompletedTask;
		}

		return LoopAsync(token);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Computes SHA-256 of <paramref name="data" />.
	/// </summary>
	private static byte[] ComputeHash(ReadOnlySpan<byte> data) => SHA256.HashData(data);

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
			_logger.LogWarning(ex.Message);

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
			_logger.LogWarning(ex.Message);

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
			_logger.LogWarning(ex.Message);

			return null;
		}
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

			// Text first — overwhelmingly the common case.
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
			}
		}
		catch (Exception ex)
		{
			// Polling errors must never tear the timer down.
			_logger.LogDebug($"Clipboard poll failed: {ex.Message}");
		}
	}
	#endregion
}
