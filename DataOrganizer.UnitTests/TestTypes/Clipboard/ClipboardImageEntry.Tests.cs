using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using DataOrganizer.Helpers;
using Shared.Properties;

namespace DataOrganizer.UnitTests.TestTypes.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardImageEntry)}"" type")]
internal class ClipboardImageEntryTests
{
	#region Methods
	/// <summary>
	/// Test of the image badge metadata.
	/// </summary>
	[Test]
	public void Badge_Metadata_Is_Image_Specific()
	{
		// Arrange
		ClipboardImageEntry sut = ImageEntry([]);

		// Act, Assert
		sut.TypeGlyph
			.Should()
			.Be(Glyphs.FramedPicture);

		sut.TypeToolTip
			.Should()
			.Be(Strings.Image);
	}

	/// <summary>
	/// <see cref="ClipboardImageEntry.ContentToolTip" />: malformed bytes yield no size.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_For_Malformed_Png()
	{
		// Arrange
		ClipboardImageEntry sut = ImageEntry([0, 1, 2, 3]);

		// Act, Assert
		sut.ContentToolTip
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardImageEntry.ContentToolTip" />: the size is read from the PNG IHDR header.
	/// </summary>
	[Test]
	public void ContentToolTip_Reports_Png_Pixel_Size()
	{
		// Arrange
		ClipboardImageEntry sut = ImageEntry(PngWithSize(width: 100, height: 50));

		// Act, Assert
		sut.ContentToolTip
			.Should()
			.Be("100 × 50");
	}

	/// <summary>
	/// <see cref="ClipboardImageEntry.ImagePreview" />: empty bytes produce no preview.
	/// </summary>
	[Test]
	public void ImagePreview_Is_Null_For_Empty_Bytes()
	{
		// Arrange
		ClipboardImageEntry sut = ImageEntry([]);

		// Act, Assert
		sut.ImagePreview
			.Should()
			.BeNull();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// An image entry backed by <paramref name="png" />.
	/// </summary>
	private static ClipboardImageEntry ImageEntry(byte[] png) => new()
	{
		OriginalPng = png,
		Hash = [1]
	};

	/// <summary>
	/// Builds a minimal PNG header (signature + IHDR with the given size); pixel data is not included.
	/// </summary>
	private static byte[] PngWithSize(int width, int height) =>
	[
		// 8-byte PNG signature.
		137, 80, 78, 71, 13, 10, 26, 10,
		// IHDR chunk length (13).
		0, 0, 0, 13,
		// "IHDR".
		73, 72, 68, 82,
		// Width (big-endian).
		(byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
		// Height (big-endian).
		(byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height
	];
	#endregion
}
