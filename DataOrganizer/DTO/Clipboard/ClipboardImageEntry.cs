using Avalonia.Media.Imaging;
using System.IO;

namespace DataOrganizer.DTO.Clipboard;

/// <summary>
/// Image clipboard entry backed by the original full-size PNG bytes.
/// </summary>
public sealed class ClipboardImageEntry : ClipboardHistoryEntryBase
{
	#region Properties
	/// <summary>
	/// Lazily-built downscaled bitmap for display.
	/// </summary>
	public Bitmap? ImagePreview => field ??= BuildImagePreview();

	/// <summary>
	/// Original full-size PNG bytes.
	/// </summary>
	public required byte[] OriginalPng { get; init; }
	#endregion

	#region Data
	/// <summary>
	/// Longest side of the cached preview in device-independent pixels.
	/// </summary>
	private const int PreviewMaxSide = 160;
	#endregion

	#region Helpers
	/// <summary>
	/// Decodes <see cref="OriginalPng" /> into a downscaled <see cref="Bitmap" />.
	/// </summary>
	private Bitmap? BuildImagePreview()
	{
		if (OriginalPng.Length == 0)
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
