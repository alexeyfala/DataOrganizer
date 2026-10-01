using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Bogus;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Models.Clipboard;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Contents of the clipboard made up for trying the clipboard history: texts of every kind it tells apart, links, images
/// and lists of files.
/// </summary>
internal static class SampleClipboardContents
{
	#region Data
	/// <summary>
	/// Number of bytes of a pixel of the images.
	/// </summary>
	private const int BytesPerPixel = 4;

	/// <summary>
	/// Color of the disc on the transparent image.
	/// </summary>
	private const uint DiscColor = 0xFF2E7D32;

	/// <summary>
	/// Resolution of the images, in dots per inch.
	/// </summary>
	private const double Dpi = 96.0;

	/// <summary>
	/// Level of the color channel that the gradients keep the same.
	/// </summary>
	private const byte GradientLevel = 128;

	/// <summary>
	/// Domain of the links, reserved for examples, so that no link leads to a real site.
	/// </summary>
	private const string LinkDomain = "example.com";

	/// <summary>
	/// Number of words in the path of the long link.
	/// </summary>
	private const int LongLinkWordCount = 100;

	/// <summary>
	/// Number of files in the long list of files.
	/// </summary>
	private const int ManyFileCount = 30;

	/// <summary>
	/// Number of files in the list of folders and files.
	/// </summary>
	private const int MixedFileCount = 5;

	/// <summary>
	/// Number of folders in the list of folders and files.
	/// </summary>
	private const int MixedFolderCount = 3;

	/// <summary>
	/// Length of the text that looks like a password.
	/// </summary>
	private const int PasswordLength = 16;

	/// <summary>
	/// Name of the made-up folder among the documents of the user that holds the listed files.
	/// </summary>
	private const string SamplesFolderName = "Clipboard samples";

	/// <summary>
	/// Color of a transparent pixel.
	/// </summary>
	private const uint TransparentColor = 0x00000000;

	/// <summary>
	/// Colors of the bars of the wide image, from left to right.
	/// </summary>
	private static readonly uint[] BarColors = [0xFFFFFFFF, 0xFFFFFF00, 0xFF00FFFF, 0xFF00FF00, 0xFFFF00FF, 0xFFFF0000, 0xFF0000FF, 0xFF000000];
	#endregion

	#region Methods
	/// <summary>
	/// Returns lists of files and folders that do not exist, from a single item to more than a card of the history shows.
	/// </summary>
	public static ClipboardFileSystemEntry[][] CreateFileLists(Faker faker)
	{
		string folder = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			SamplesFolderName);

		ClipboardFileSystemEntry[] mixed =
		[
			.. faker.Lorem.Words(MixedFolderCount).Select(x => CreateFolder(folder, x)),
			.. Enumerable.Range(0, MixedFileCount).Select(_ => CreateFile(folder, faker.System.CommonFileName()))
		];

		return
		[
			[CreateFile(folder, faker.System.CommonFileName())],
			[CreateFolder(folder, faker.Lorem.Word())],
			// Folders first, then files, each group by name, as a captured list is ordered.
			[.. mixed.OrderByDescending(static x => x.IsFolder).ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)],
			[.. Enumerable.Range(1, ManyFileCount).Select(x => CreateFile(folder, $"photo {x:00}.jpg"))]
		];
	}

	/// <summary>
	/// Returns images in PNG of several shapes, one of them with transparent pixels.
	/// </summary>
	public static byte[][] CreateImages()
	{
		// One row is one image.
		(int Width, int Height, Func<int, int, int, int, uint> Paint)[] images =
		[
			(64, 64, PaintDisc),
			(1600, 400, PaintBars),
			(400, 1200, PaintVerticalGradient),
			(1920, 1080, PaintDiagonalGradient)
		];

		return [.. images.Select(static x => CreatePng(x.Width, x.Height, x.Paint))];
	}

	/// <summary>
	/// Returns texts of every kind the history tells apart: plain, formatted in HTML or RTF, secret, and links.
	/// </summary>
	public static (string Text, string? Html, string? Rtf)[] CreateTexts(Faker faker)
	{
		string html = faker.Lorem.Sentence();

		string rtf = faker.Lorem.Sentence();

		string htmlAndRtf = faker.Lorem.Sentence();

		return
		[
			(faker.Lorem.Sentence(), null, null),
			(SampleDocuments.CreateText(faker), null, null),
			(SampleDocuments.CreateRussianText(faker), null, null),
			($"{faker.Lorem.Sentence()} \U0001F389 \U0001F44D", null, null),
			(SampleText.CSharp, null, null),
			(faker.Internet.Password(PasswordLength), null, null),
			(html, CreateHtml(html), null),
			(rtf, null, CreateRtf(rtf)),
			(htmlAndRtf, CreateHtml(htmlAndRtf), CreateRtf(htmlAndRtf)),
			// A link inside a sentence leaves it a text.
			($"{faker.Lorem.Sentence()} {CreateLink(faker, "https")} {faker.Lorem.Sentence()}", null, null),
			(CreateLink(faker, "https"), null, null),
			($"{CreateLink(faker, "http")}?q={faker.Lorem.Word()}&lang=en", null, null),
			($"https://{LinkDomain}/{string.Join('/', faker.Lorem.Words(LongLinkWordCount))}", null, null),
			// The spaces around a link are trimmed when it is recognized.
			($"  {CreateLink(faker, "https")}  ", null, null)
		];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns an item of a list that stands for a file.
	/// </summary>
	private static ClipboardFileSystemEntry CreateFile(string folder, string name) => new(
		Path.Combine(folder, name),
		IsFolder: false);

	/// <summary>
	/// Returns an item of a list that stands for a folder.
	/// </summary>
	private static ClipboardFileSystemEntry CreateFolder(string folder, string name) => new(
		Path.Combine(folder, name),
		IsFolder: true);

	/// <summary>
	/// Returns the HTML of a text in bold.
	/// </summary>
	private static string CreateHtml(string text) => $"<p><b>{text}</b></p>";

	/// <summary>
	/// Returns a link to a page of the domain for examples.
	/// </summary>
	private static string CreateLink(Faker faker, string protocol) => faker.Internet.UrlWithPath(protocol, LinkDomain);

	/// <summary>
	/// Draws an image pixel by pixel and returns it in PNG.
	/// </summary>
	private static byte[] CreatePng(
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

	/// <summary>
	/// Returns the RTF of a text in bold.
	/// </summary>
	private static string CreateRtf(string text) => $$"""{\rtf1\ansi {\b {{text}}}\par}""";

	/// <summary>
	/// Returns an opaque ARGB color.
	/// </summary>
	private static uint FromRgb(byte red, byte green, byte blue) => 0xFF000000 | ((uint)red << 16) | ((uint)green << 8) | blue;

	/// <summary>
	/// Returns the color of a pixel of upright bars of pure colors, like a test pattern.
	/// </summary>
	private static uint PaintBars(int x, int y, int width, int height) => BarColors[x * BarColors.Length / width];

	/// <summary>
	/// Returns the color of a pixel of a gradient whose red grows to the right and green grows downwards.
	/// </summary>
	private static uint PaintDiagonalGradient(int x, int y, int width, int height) => FromRgb(
		(byte)(x * byte.MaxValue / (width - 1)),
		(byte)(y * byte.MaxValue / (height - 1)),
		GradientLevel);

	/// <summary>
	/// Returns the color of a pixel of a disc on a transparent background.
	/// </summary>
	private static uint PaintDisc(int x, int y, int width, int height)
	{
		double radius = Math.Min(width, height) / 2.0;

		double dx = x + 0.5 - (width / 2.0);

		double dy = y + 0.5 - (height / 2.0);

		return (dx * dx) + (dy * dy) <= radius * radius ? DiscColor : TransparentColor;
	}

	/// <summary>
	/// Returns the color of a pixel of a gradient from blue at the top to orange at the bottom.
	/// </summary>
	private static uint PaintVerticalGradient(int x, int y, int width, int height)
	{
		byte level = (byte)(y * byte.MaxValue / (height - 1));

		return FromRgb(
			level,
			GradientLevel,
			(byte)(byte.MaxValue - level));
	}
	#endregion
}
