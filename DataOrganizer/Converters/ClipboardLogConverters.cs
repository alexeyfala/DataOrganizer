using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using DataOrganizer.Models.Clipboard;
using Shared.Properties;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TextProcessing.Constants;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the entries of the clipboard log, referenced via x:Static.
/// </summary>
internal static class ClipboardLogConverters
{
	#region Data
	/// <summary>
	/// Most characters of the name of an entry for UI Automation; the rest of a long text is cut.
	/// </summary>
	private const int AutomationNameMaxLength = 100;

	/// <summary>
	/// Total maximum number of lines rendered by the preview of a list of files.
	/// </summary>
	private const int FilesPreviewMaxLines = 7;

	/// <summary>
	/// Total maximum number of lines rendered by the tooltip of a list of files
	/// (full expanded list, capped to avoid a screen-tall tooltip).
	/// </summary>
	private const int FilesToolTipMaxLines = 22;

	/// <summary>
	/// Maximum characters per wrapped line of the tooltip of a URL.
	/// </summary>
	private const int UrlToolTipMaxLineLength = 64;

	/// <summary>
	/// Maximum number of lines rendered by the tooltip of a URL.
	/// </summary>
	private const int UrlToolTipMaxLines = 10;
	#endregion

	#region Properties
	/// <summary>
	/// Name of an entry for UI Automation: its text or the names of its files on one line, the size of an image, or a
	/// neutral word for a secret, which the log blurs.
	/// </summary>
	public static FuncValueConverter<ClipboardLogEntryBase?, string?> AutomationName { get; } = new(GetAutomationName);

	/// <summary>
	/// Tooltip of the contents of an entry: the pixel size of an image, the wrapped URL of a link, or the list of files
	/// the preview cuts; <c>null</c> for the rest.
	/// </summary>
	public static FuncValueConverter<ClipboardLogEntryBase?, string?> ContentToolTip { get; } = new(GetContentToolTip);

	/// <summary>
	/// Multi-line display block of a list of files: the counts, then a line per item.
	/// </summary>
	public static FuncValueConverter<IReadOnlyList<ClipboardFileSystemEntry>?, string?> FilesPreview { get; } =
		new(GetFilesPreview);

	/// <summary>
	/// Image of a PNG decoded at the width given as the parameter; <c>null</c> without one or when it does not decode.
	/// </summary>
	public static FuncValueConverter<byte[]?, double, Bitmap?> PngToPreview { get; } = new(GetPreview);

	/// <summary>
	/// Emoji glyph of the kind of an entry.
	/// </summary>
	public static FuncValueConverter<ClipboardLogEntryBase?, string?> TypeGlyph { get; } = new(GetTypeGlyph);

	/// <summary>
	/// Human-readable kind of an entry, with the formats of a text or the counts of a list of files.
	/// </summary>
	public static FuncValueConverter<ClipboardLogEntryBase?, string?> TypeToolTip { get; } = new(GetTypeToolTip);
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the lines of the summary of a list of files on demand.
	/// </summary>
	private static IEnumerable<string> EnumerateFilesSummaryLines(
		IReadOnlyList<ClipboardFileSystemEntry> entries,
		int maxLines)
	{
		const int headerLines = 1;

		int maxItemLines = maxLines - headerLines;

		bool truncated = entries.Count > maxItemLines;

		int visibleCount = truncated ? maxItemLines - 1 : entries.Count;

		int folderCount = entries.Count(e => e.IsFolder);

		const string folderGlyph = Glyphs.FileFolder;

		const string fileGlyph = Glyphs.PageFacingUp;

		const string bulletGlyph = Glyphs.MiddleDot;

		yield return $"{folderGlyph}: {folderCount}  {fileGlyph}: {entries.Count - folderCount}  Σ: {entries.Count}";

		foreach (ClipboardFileSystemEntry entry in entries.Take(visibleCount))
		{
			yield return $"{bulletGlyph}  {(entry.IsFolder ? folderGlyph : fileGlyph)}  {entry.Name}";
		}

		if (truncated)
		{
			yield return "...";
		}
	}

	/// <summary>
	/// Wraps <paramref name="url" /> at <see cref="UrlToolTipMaxLineLength" /> chars,
	/// caps it at <see cref="UrlToolTipMaxLines" /> lines and appends "..." when truncated.
	/// </summary>
	private static IEnumerable<string> EnumerateUrlToolTipLines(string url)
	{
		int totalLines = (url.Length + UrlToolTipMaxLineLength - 1) / UrlToolTipMaxLineLength;

		bool truncated = totalLines > UrlToolTipMaxLines;

		int visibleLines = truncated ? UrlToolTipMaxLines - 1 : totalLines;

		for (int i = 0; i < visibleLines; i++)
		{
			int start = i * UrlToolTipMaxLineLength;

			int length = Math.Min(UrlToolTipMaxLineLength, url.Length - start);

			yield return url.Substring(start, length);
		}

		if (truncated)
		{
			yield return "...";
		}
	}

	/// <summary>
	/// Returns the name of an entry for UI Automation; <c>null</c> without an entry.
	/// </summary>
	private static string? GetAutomationName(ClipboardLogEntryBase? entry)
	{
		return entry switch
		{
			{ IsSensitive: true } => Strings.HiddenText,
			ClipboardFilesEntry files => ToOneLine(GetFileNames(files.FileSystemEntries)),
			ClipboardImageEntry image => GetPngSize(image.OriginalPng) is { } size ? $"{Strings.Image} {size}" : Strings.Image,
			ClipboardTextEntryBase text => ToOneLine(text.Text),
			_ => null
		};
	}

	/// <summary>
	/// Returns the tooltip of the contents of an entry; <c>null</c> for an entry without one.
	/// </summary>
	private static string? GetContentToolTip(ClipboardLogEntryBase? entry)
	{
		return entry switch
		{
			ClipboardFilesEntry files => GetFilesToolTip(files.FileSystemEntries),
			ClipboardImageEntry image => GetPngSize(image.OriginalPng),
			ClipboardUrlEntry link => GetUrlToolTip(link.Url),
			_ => null
		};
	}

	/// <summary>
	/// Returns the names of the items of a list of files, separated by commas.
	/// </summary>
	private static string GetFileNames(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		return string.Join(", ", entries.Select(static x => x.Name));
	}

	/// <summary>
	/// Returns the multi-line display block of a list of files; <c>null</c> without one.
	/// </summary>
	private static string? GetFilesPreview(IReadOnlyList<ClipboardFileSystemEntry>? entries)
	{
		if (entries is null)
		{
			return null;
		}

		return string.Join(Environment.NewLine, EnumerateFilesSummaryLines(entries, FilesPreviewMaxLines));
	}

	/// <summary>
	/// Returns the expanded tooltip version of the preview of a list of files; <c>null</c> while the preview shows it
	/// whole.
	/// </summary>
	private static string? GetFilesToolTip(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		// Show the tooltip only when the visible block was truncated.
		const int headerLines = 1;

		if (entries.Count <= FilesPreviewMaxLines - headerLines)
		{
			return null;
		}

		return string.Join(Environment.NewLine, EnumerateFilesSummaryLines(entries, FilesToolTipMaxLines));
	}

	/// <summary>
	/// Returns the kind of a list of files with the folder and file counts.
	/// </summary>
	private static string GetFilesTypeToolTip(IReadOnlyList<ClipboardFileSystemEntry> entries)
	{
		int folderCount = entries.Count(static x => x.IsFolder);

		return
			$"{Strings.Folders}: {folderCount}{Environment.NewLine}" +
			$"{Strings.Files}: {entries.Count - folderCount}{Environment.NewLine}" +
			$"Σ: {entries.Count}";
	}

	/// <summary>
	/// Returns the original pixel size of <paramref name="png" /> as "W × H", or <c>null</c>.
	/// </summary>
	private static string? GetPngSize(byte[] png)
	{
		return TryReadPngSize(png, out int width, out int height)
			? $"{width} × {height}"
			: null;
	}

	/// <summary>
	/// Returns the image of <paramref name="png" /> decoded at <paramref name="width" />; <c>null</c> without one or when it
	/// does not decode.
	/// </summary>
	private static Bitmap? GetPreview(byte[]? png, double width)
	{
		if (png is not { Length: > 0 })
		{
			return null;
		}

		try
		{
			using MemoryStream stream = new(png);

			// DecodeToWidth keeps aspect ratio and avoids decoding the full-size image.
			return Bitmap.DecodeToWidth(stream, (int)width);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Returns the glyph of a text by its companion formats.
	/// </summary>
	private static string GetTextGlyph(ClipboardTextEntry text)
	{
		const string htmlGlyph = Glyphs.AngleBracketSlash;

		const string formattedTextGlyph = Glyphs.BButton;

		const string plainTextGlyph = Glyphs.InputLatinLetters;

		return (text.IsHtml, text.IsRtf) switch
		{
			(true, true) => $"{htmlGlyph} {formattedTextGlyph}",
			(true, false) => htmlGlyph,
			(false, true) => formattedTextGlyph,
			_ => plainTextGlyph
		};
	}

	/// <summary>
	/// Returns the kind of a text matching <see cref="GetTextGlyph" />.
	/// </summary>
	private static string GetTextTypeToolTip(ClipboardTextEntry text)
	{
		return (text.IsHtml, text.IsRtf) switch
		{
			(true, true) => $"HTML + {Strings.FormattedText}",
			(true, false) => "HTML",
			(false, true) => Strings.FormattedText,
			_ => Strings.PlainText
		};
	}

	/// <summary>
	/// Returns the glyph of the kind of an entry; <c>null</c> without an entry.
	/// </summary>
	private static string? GetTypeGlyph(ClipboardLogEntryBase? entry)
	{
		return entry switch
		{
			ClipboardFilesEntry => Glyphs.CardIndexDividers,
			ClipboardImageEntry => Glyphs.FramedPicture,
			ClipboardTextEntry text => GetTextGlyph(text),
			ClipboardUrlEntry => Glyphs.Link,
			_ => null
		};
	}

	/// <summary>
	/// Returns the human-readable kind of an entry; <c>null</c> without an entry.
	/// </summary>
	private static string? GetTypeToolTip(ClipboardLogEntryBase? entry)
	{
		return entry switch
		{
			ClipboardFilesEntry files => GetFilesTypeToolTip(files.FileSystemEntries),
			ClipboardImageEntry => Strings.Image,
			ClipboardTextEntry text => GetTextTypeToolTip(text),
			ClipboardUrlEntry => Strings.Hyperlink,
			_ => null
		};
	}

	/// <summary>
	/// Returns the wrapped, length-capped tooltip of <paramref name="url" />.
	/// </summary>
	private static string GetUrlToolTip(string url) => string.Join(Environment.NewLine, EnumerateUrlToolTipLines(url));

	/// <summary>
	/// Returns <paramref name="text" /> on one line, each run of white space as one space, cut at
	/// <see cref="AutomationNameMaxLength" /> characters with an ellipsis.
	/// </summary>
	private static string ToOneLine(string text)
	{
		StringBuilder line = new(AutomationNameMaxLength + 3);

		bool isSpaceDue = false;

		// The walk stops at the limit, so a text of megabytes costs no more than a short one.
		foreach (char character in text)
		{
			if (char.IsWhiteSpace(character))
			{
				isSpaceDue = line.Length > 0;

				continue;
			}

			if (line.Length + (isSpaceDue ? 1 : 0) >= AutomationNameMaxLength)
			{
				return line
					.Append("...")
					.ToString();
			}

			if (isSpaceDue)
			{
				line.Append(' ');

				isSpaceDue = false;
			}

			line.Append(character);
		}

		return line.ToString();
	}

	/// <summary>
	/// Reads the image size from the PNG IHDR header without decoding the pixels.
	/// </summary>
	private static bool TryReadPngSize(
		ReadOnlySpan<byte> png,
		out int width,
		out int height)
	{
		width = 0;

		height = 0;

		// 8-byte signature + 4-byte chunk length + "IHDR"; width at offset 16, height at 20.
		if (png.Length < 24 || !png.Slice(12, 4).SequenceEqual("IHDR"u8))
		{
			return false;
		}

		width = (int)BinaryPrimitives.ReadUInt32BigEndian(png.Slice(16, 4));

		height = (int)BinaryPrimitives.ReadUInt32BigEndian(png.Slice(20, 4));

		return width > 0 && height > 0;
	}
	#endregion
}
