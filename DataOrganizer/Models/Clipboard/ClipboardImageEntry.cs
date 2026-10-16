using DataOrganizer.Helpers;
using Shared.Properties;
using System;
using System.Buffers.Binary;

namespace DataOrganizer.Models.Clipboard;

/// <summary>
/// Image clipboard entry backed by the original full-size PNG bytes.
/// </summary>
public sealed class ClipboardImageEntry : ClipboardLogEntryBase
{
	#region Properties
	/// <inheritdoc />
	public override string? ContentToolTip => field ??= BuildContentToolTip();

	/// <summary>
	/// Original full-size PNG bytes.
	/// </summary>
	public required byte[] OriginalPng { get; init; }

	/// <inheritdoc />
	public override string TypeGlyph => Glyphs.FramedPicture;

	/// <inheritdoc />
	public override string TypeToolTip => Strings.Image;
	#endregion

	#region Helpers
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

	/// <summary>
	/// Returns the original pixel size of <see cref="OriginalPng" /> as "W × H", or <c>null</c>.
	/// </summary>
	private string? BuildContentToolTip()
	{
		return TryReadPngSize(OriginalPng, out int width, out int height)
			? $"{width} × {height}"
			: null;
	}
	#endregion
}
