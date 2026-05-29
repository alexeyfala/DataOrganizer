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
	/// File / folder items captured for kind <see cref="ClipboardEntryKind.FileSystemEntries" />. <c>null</c> for other kinds.
	/// </summary>
	public IReadOnlyList<ClipboardFileSystemEntry>? FileSystemEntries { get; init; }

	/// <summary>
	/// SHA-256 of the source data; used for change detection and deduplication.
	/// </summary>
	public required byte[] Hash { get; init; }

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
	/// Plain text content. <c>null</c> for image entries.
	/// </summary>
	public string? Text { get; init; }

	/// <summary>
	/// Wall-clock time when this entry was captured.
	/// </summary>
	public required DateTimeOffset Timestamp { get; init; }
	#endregion

	#region Data
	/// <summary>
	/// Total maximum number of lines rendered by the files-summary block.
	/// </summary>
	private const int FilesSummaryMaxLines = 5;

	/// <summary>
	/// Longest side of the cached preview in device-independent pixels.
	/// </summary>
	private const int PreviewMaxSide = 160;
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a multi-line display block for <see cref="FileSystemEntries" />.
	/// </summary>
	private string? BuildFilesSummary()
	{
		if (Kind != ClipboardEntryKind.FileSystemEntries || FileSystemEntries is not { Count: > 0 })
		{
			return null;
		}

		const int headerLines = 1;

		const int maxItemLines = FilesSummaryMaxLines - headerLines;

		bool truncated = FileSystemEntries.Count > maxItemLines;

		int visibleCount = truncated ? maxItemLines - 1 : FileSystemEntries.Count;

		List<string> lines = new(FilesSummaryMaxLines)
		{
			$"{FileSystemEntries.Count} entries:",
		};

		foreach (ClipboardFileSystemEntry entry in FileSystemEntries.Take(visibleCount))
		{
			lines.Add($"{(entry.IsFolder ? "📁" : "📄")}  {entry.Name}");
		}

		if (truncated)
		{
			lines.Add($"+{FileSystemEntries.Count - visibleCount} more");
		}

		return string.Join(Environment.NewLine, lines);
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
	#endregion
}
