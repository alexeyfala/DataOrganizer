using Avalonia.Media.Imaging;
using DataOrganizer.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataOrganizer.DTO;

/// <summary>
/// One record in the in-memory system clipboard history.
/// </summary>
public sealed class ClipboardHistoryEntry
{
	#region Properties
	/// <summary>
	/// Pre-computed display line for kind <see cref="ClipboardEntryKind.FileSystemEntries" />.
	/// </summary>
	public string? FilesSummary => field ??= BuildFilesSummary();

	/// <summary>
	/// Pre-computed expanded list shown as a tooltip when <see cref="FilesSummary" /> is truncated.
	/// </summary>
	public string? FilesSummaryToolTip => field ??= BuildFilesSummaryToolTip();

	/// <summary>
	/// File / folder items captured for kind <see cref="ClipboardEntryKind.FileSystemEntries" />. <c>null</c> for other kinds.
	/// </summary>
	public IReadOnlyList<ClipboardFileSystemEntry>? FileSystemEntries { get; init; }

	/// <summary>
	/// SHA-256 of the source data; used for change detection and deduplication.
	/// </summary>
	public required byte[] Hash { get; init; }

	/// <summary>
	/// HTML version of <see cref="Text" /> (e.g. from browsers or Word) when the
	/// source app provided one. Pushed back to the clipboard alongside plain text
	/// on restore so paste targets can pick up the formatting.
	/// </summary>
	public string? Html { get; init; }

	/// <summary>
	/// <c>True</c> when <see cref="Url" /> is set.
	/// </summary>
	public bool IsUrl => Url is not null;

	/// <inheritdoc cref="ClipboardEntryKind" />
	public required ClipboardEntryKind Kind { get; init; }

	/// <summary>
	/// Original full-size PNG bytes. <c>null</c> for text entries.
	/// </summary>
	public byte[]? OriginalPng { get; init; }

	/// <summary>
	/// Lazily-built downscaled bitmap for display. <c>null</c> for text entries.
	/// </summary>
	public Bitmap? Preview => field ??= BuildPreview();

	/// <summary>
	/// RTF version of <see cref="Text" /> when the source app provided one.
	/// </summary>
	public string? Rtf { get; init; }

	/// <summary>
	/// Plain text content. <c>null</c> for image entries.
	/// </summary>
	public string? Text { get; init; }

	/// <summary>
	/// Trimmed <see cref="Text" /> when it matches an absolute http(s) URL
	/// (whole-string match). <c>null</c> for non-URL text and other kinds.
	/// </summary>
	public string? Url { get; init; }
	#endregion

	#region Data
	/// <summary>
	/// Total maximum number of lines rendered by the files-summary block.
	/// </summary>
	private const int FilesSummaryMaxLines = 7;

	/// <summary>
	/// Total maximum number of lines rendered by the files-summary tooltip
	/// (full expanded list, capped to avoid a screen-tall tooltip).
	/// </summary>
	private const int FilesSummaryToolTipMaxLines = 22;

	/// <summary>
	/// Longest side of the cached preview in device-independent pixels.
	/// </summary>
	private const int PreviewMaxSide = 160;
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the lines of the files-summary block on demand.
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

		const string folderGlyph = "📁";

		const string fileGlyph = "📄";

		const string bulletGlyph = "·";

		yield return $"{folderGlyph} {folderCount}  {fileGlyph} {entries.Count - folderCount}";

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
	/// Builds a multi-line display block for <see cref="FileSystemEntries" />.
	/// </summary>
	private string? BuildFilesSummary()
	{
		if (GetSummaryEntries() is not { } entries)
		{
			return null;
		}

		return string.Join(Environment.NewLine, EnumerateFilesSummaryLines(entries, FilesSummaryMaxLines));
	}

	/// <summary>
	/// Builds the expanded tooltip version of <see cref="FilesSummary" />.
	/// </summary>
	private string? BuildFilesSummaryToolTip()
	{
		if (GetSummaryEntries() is not { } entries)
		{
			return null;
		}

		// Show the tooltip only when the visible block was truncated.
		const int headerLines = 1;

		if (entries.Count <= FilesSummaryMaxLines - headerLines)
		{
			return null;
		}

		return string.Join(Environment.NewLine, EnumerateFilesSummaryLines(entries, FilesSummaryToolTipMaxLines));
	}

	/// <summary>
	/// Decodes <see cref="OriginalPng" /> into a downscaled <see cref="Bitmap" />.
	/// </summary>
	private Bitmap? BuildPreview()
	{
		if (Kind != ClipboardEntryKind.Image
			|| OriginalPng is null
			|| OriginalPng.Length == 0)
		{
			return null;
		}

		try
		{
			using MemoryStream stream = new(OriginalPng);

			// DecodeToWidth keeps aspect ratio and avoids decoding the full-size image.
			return Bitmap.DecodeToWidth(stream, PreviewMaxSide);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>Non-empty <see cref="FileSystemEntries" /> or <c>null</c>.</summary>
	private IReadOnlyList<ClipboardFileSystemEntry>? GetSummaryEntries()
	{
		return Kind == ClipboardEntryKind.FileSystemEntries && FileSystemEntries is { Count: > 0 } entries
			? entries
			: null;
	}
	#endregion
}
