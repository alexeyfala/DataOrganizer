using Avalonia.Media.Imaging;
using DataOrganizer.Enums;
using System;
using System.IO;

namespace DataOrganizer.DTO;

/// <summary>
/// One record in the in-memory system clipboard history.
/// </summary>
public sealed class ClipboardHistoryEntry
{
	#region Properties
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

	/// <inheritdoc cref="Preview" />
	private Bitmap? _preview;
	#endregion

	#region Helpers
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
