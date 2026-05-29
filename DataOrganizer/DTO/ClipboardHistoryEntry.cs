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
	/// File / folder items captured for kind <see cref="ClipboardEntryKind.Files" />. <c>null</c> for other kinds.
	/// </summary>
	public IReadOnlyList<ClipboardFileEntry>? FileEntries { get; init; }

	/// <summary>
	/// Pre-computed display line for kind <see cref="ClipboardEntryKind.Files" /> —
	/// "N entries: name1, name2, ...". <c>null</c> for other kinds.
	/// </summary>
	public string? FilesSummary => _filesSummary ??= BuildFilesSummary();

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
	public Bitmap? Preview => _preview ??= BuildPreview();

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
	/// Longest side of the cached preview in device-independent pixels.
	/// </summary>
	private const int PreviewMaxSide = 160;

	/// <inheritdoc cref="FilesSummary" />
	private string? _filesSummary;

	/// <inheritdoc cref="Preview" />
	private Bitmap? _preview;
	#endregion

	#region Helpers
	/// <summary>
	/// Builds the display line "N entries: name1, name2, ..." from <see cref="FileEntries" />.
	/// </summary>
	private string? BuildFilesSummary()
	{
		if (Kind != ClipboardEntryKind.Files || FileEntries is not { Count: > 0 })
		{
			return null;
		}

		IEnumerable<string> names = FileEntries.Select(entry =>
		{
			string trimmed = entry.Path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			string name = Path.GetFileName(trimmed);

			return string.IsNullOrEmpty(name) ? trimmed : name;
		});

		return $"{FileEntries.Count} entries: {string.Join(", ", names)}";
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
