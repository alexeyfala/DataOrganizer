using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Clipboard.Interfaces;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace DataOrganizer.Services.Clipboard;

/// <inheritdoc cref="IPngPainter" />
public sealed class PngPainter : IPngPainter
{
	#region Data
	/// <summary>
	/// Number of bytes of a pixel of the images.
	/// </summary>
	private const int BytesPerPixel = 4;

	/// <summary>
	/// Resolution of the images, in dots per inch.
	/// </summary>
	private const double Dpi = 96.0;
	#endregion

	#region Methods
	/// <inheritdoc />
	public byte[] Paint(
		int width,
		int height,
		Func<int, int, int, int, uint> paint)
	{
		using WriteableBitmap bitmap = new(
			new PixelSize(width, height),
			new Vector(Dpi, Dpi),
			PixelFormat.Bgra8888,
			AlphaFormat.Unpremul);

		using (ILockedFramebuffer framebuffer = bitmap.Lock())
		{
			byte[] row = new byte[width * BytesPerPixel];

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					// The little-endian form of an ARGB color is blue, green, red and alpha, the order of the format.
					BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(x * BytesPerPixel), paint(x, y, width, height));
				}

				Marshal.Copy(
					row,
					0,
					framebuffer.Address + (y * framebuffer.RowBytes),
					row.Length);
			}
		}

		using MemoryStream stream = new();

		bitmap.Save(stream, PngBitmapEncoderOptions.Default);

		return stream.ToArray();
	}
	#endregion
}
