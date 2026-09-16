using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using Shared.Properties;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardImageEntry)}"" type")]
internal class ClipboardImageEntryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardImageEntry.ContentToolTip" />: malformed bytes yield no size.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_For_Malformed_Png()
	{
		// Arrange
		ClipboardImageEntry sut = ClipboardEntryFactory.CreateImageEntry([0, 1, 2, 3]);

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
		ClipboardImageEntry sut = ClipboardEntryFactory.CreateImageEntry(PngWithSize(width: 100, height: 50));

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
		ClipboardImageEntry sut = ClipboardEntryFactory.CreateImageEntry([]);

		// Act, Assert
		sut.ImagePreview
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardImageEntry.TypeGlyph" />, <see cref="ClipboardImageEntry.TypeToolTip" />: the badge names an image.
	/// </summary>
	[Test]
	public void TypeGlyph_And_TypeToolTip_Are_Image_Specific()
	{
		// Arrange
		ClipboardImageEntry sut = ClipboardEntryFactory.CreateImageEntry([]);

		// Act, Assert
		sut.TypeGlyph
			.Should()
			.Be(Glyphs.FramedPicture);

		sut.TypeToolTip
			.Should()
			.Be(Strings.Image);
	}
	#endregion

	#region Helpers
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
