using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Clipboard.Helpers;
using Clipboard.Interfaces;
using Clipboard.Models;
using DataOrganizer.Helpers.Clipboard;
using DataOrganizer.Interfaces;
using Serilog;
using Shared.Extensions;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TextProcessing.Constants;

namespace DataOrganizer.Services.Clipboard;

public sealed class ClipboardAccessor : IClipboardAccessor
{
	#region Data
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

	/// <inheritdoc cref="IAvaloniaClipboard" />
	private readonly IAvaloniaClipboard _clipboard;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;

	/// <inheritdoc cref="IStorageAccessor" />
	private readonly IStorageAccessor _storage;
	#endregion

	#region Constructors
	public ClipboardAccessor(
		IAvaloniaClipboard clipboard,
		ILogger logger,
		IStorageAccessor storage)
	{
		_clipboard = clipboard;

		_logger = logger;

		_storage = storage;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task ClearAsync() => _clipboard.ClearAsync();

	/// <inheritdoc />
	public async Task<bool> ContainsOwnershipMarkerAsync()
	{
		IReadOnlyList<DataFormat> formats = await _clipboard
			.GetDataFormatsAsync()
			.ConfigureAwait(false);

		return ClipboardSensitivityMarkerWriter.ContainsOwnershipMarker(formats);
	}

	/// <inheritdoc />
	public async Task<bool> ContainsRestoreMarkerAsync()
	{
		IReadOnlyList<DataFormat> formats = await _clipboard
			.GetDataFormatsAsync()
			.ConfigureAwait(false);

		return ClipboardRestoreMarker.IsAmong(formats);
	}

	/// <inheritdoc />
	public async Task<bool> ContainsSensitivityMarkerAsync()
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
			_logger.LogException(ex, breakInDebugger: false);

			return false;
		}
	}

	/// <inheritdoc />
	public async Task<bool> SetFilesAsync(IReadOnlyList<ClipboardFileSystemEntry> entries, bool isRestored)
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
					_logger.LogException(ex, breakInDebugger: false);
				}
			}

			if (resolved.Count == 0)
			{
				return false;
			}

			// Do NOT dispose: ownership of the DataTransfer passes to the clipboard (delayed rendering).
			DataTransfer transfer = new();

			foreach (IStorageItem item in resolved)
			{
				transfer.Add(DataTransferItem.CreateFile(item));
			}

			// One marked item is enough: the clipboard lists the formats of all items.
			if (isRestored)
			{
				ClipboardRestoreMarker.Attach(transfer.Items[0]);
			}

			// On Linux file managers paste files from x-special/gnome-copied-files, not from the
			// text/uri-list that Avalonia advertises — add it explicitly so Ctrl+V works there.
			if (GnomeCopiedFilesFormat.Value is { } gnomeFormat)
			{
				DataTransferItem gnomeItem = new();

				gnomeItem.Set(gnomeFormat, TextDefaults.Encoding.GetBytes(BuildGnomeCopiedFiles(resolved)));

				transfer.Add(gnomeItem);
			}

			await _clipboard
				.SetDataAsync(transfer)
				.ConfigureAwait(false);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return false;
		}
	}

	/// <inheritdoc />
	public async Task<bool> SetImageAsync(byte[] png, bool isRestored)
	{
		try
		{
			await using MemoryStream stream = new(png);

			// Do NOT dispose: ownership passes to the clipboard (delayed rendering) — disposing
			// here yields an empty paste. GC reclaims it once clipboard ownership changes.
			Bitmap bitmap = new(stream);

			DataTransferItem item = new();

			item.SetBitmap(bitmap);

			if (isRestored)
			{
				ClipboardRestoreMarker.Attach(item);
			}

			// Do NOT dispose: ownership of the DataTransfer passes to the clipboard (delayed rendering).
			DataTransfer transfer = new();

			transfer.Add(item);

			await _clipboard
				.SetDataAsync(transfer)
				.ConfigureAwait(false);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return false;
		}
	}

	/// <inheritdoc />
	public async Task<bool> SetTextAsync(
		string text,
		string? html,
		string? rtf,
		bool isSensitive,
		bool isRestored)
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

			if (isRestored)
			{
				ClipboardRestoreMarker.Attach(item);
			}

			// Do NOT dispose: ownership of the DataTransfer passes to the clipboard (delayed rendering).
			DataTransfer transfer = new();

			transfer.Add(item);

			await _clipboard
				.SetDataAsync(transfer)
				.ConfigureAwait(false);

			return true;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return false;
		}
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ClipboardFileSystemEntry>?> TryReadFilesAsync()
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
			_logger.LogException(ex, breakInDebugger: false);

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

	/// <inheritdoc />
	public Task<string?> TryReadHtmlAsync() => TryReadFormattedTextAsync(HtmlFormat);

	/// <inheritdoc />
	public async Task<byte[]?> TryReadImagePngAsync()
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
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}

		if (bitmap is null)
		{
			return null;
		}

		try
		{
			await using MemoryStream output = new();

			bitmap.Save(output, PngBitmapEncoderOptions.Default);

			return output.ToArray();
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}
		finally
		{
			bitmap.Dispose();
		}
	}

	/// <inheritdoc />
	public Task<string?> TryReadRtfAsync() => TryReadFormattedTextAsync(RtfFormat);

	/// <inheritdoc />
	public async Task<string?> TryReadTextAsync()
	{
		try
		{
			return await _clipboard
				.TryGetTextAsync()
				.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
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

		item.Set(format, TextDefaults.Encoding.GetBytes(payload));
	}

	/// <summary>
	/// Builds the <c>x-special/gnome-copied-files</c> payload: "copy" then one file:// URI per line.
	/// </summary>
	private static string BuildGnomeCopiedFiles(IEnumerable<IStorageItem> items)
	{
		return string.Join('\n', items.Select(static item => item.Path.AbsoluteUri).Prepend("copy"));
	}

	/// <summary>
	/// Builds the Linux GNOME copied-files platform format; <c>null</c> on non-Linux platforms.
	/// </summary>
	private static DataFormat<byte[]>? GetGnomeCopiedFilesFormat()
	{
		return OperatingSystem.IsLinux()
			? DataFormat.CreateBytesPlatformFormat("x-special/gnome-copied-files")
			: null;
	}

	/// <summary>
	/// Builds the platform-specific HTML byte[] format.
	/// </summary>
	private static DataFormat<byte[]>? GetHtmlFormat()
	{
		if (OperatingSystem.IsWindows())
		{
			return DataFormat.CreateBytesPlatformFormat("HTML Format");
		}
		else if (OperatingSystem.IsMacOS())
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
		if (OperatingSystem.IsWindows())
		{
			return DataFormat.CreateBytesPlatformFormat("Rich Text Format");
		}
		else if (OperatingSystem.IsMacOS())
		{
			return DataFormat.CreateBytesPlatformFormat("public.rtf");
		}
		else
		{
			return DataFormat.CreateBytesPlatformFormat("text/rtf");
		}
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
				: TextDefaults.Encoding.GetString(bytes);
		}
		catch (Exception ex)
		{
			_logger.LogException(ex, breakInDebugger: false);

			return null;
		}
	}
	#endregion
}
