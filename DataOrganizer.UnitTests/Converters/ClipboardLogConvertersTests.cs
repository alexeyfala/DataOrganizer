using Avalonia.Data.Converters;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using AwesomeAssertions;
using DataOrganizer.Converters;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TestSupport.Common;
using TextProcessing.Constants;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardLogConverters)}"" type")]
internal class ClipboardLogConvertersTests
{
	#region Data
	/// <summary>
	/// Name of the directory holding the listed files.
	/// </summary>
	private const string FolderName = "dir";

	/// <summary>
	/// Path of the first listed file.
	/// </summary>
	private static readonly string FirstFile = TestPath.Combine(FolderName, "a.txt");

	/// <summary>
	/// Directory holding the listed files.
	/// </summary>
	private static readonly string Folder = TestPath.Combine(FolderName);

	/// <summary>
	/// Path of the second listed file.
	/// </summary>
	private static readonly string SecondFile = TestPath.Combine(FolderName, "b.txt");
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: a text longer than the name keeps its first 100 characters and
	/// ends with an ellipsis.
	/// </summary>
	[Test]
	public void AutomationName_Cuts_A_Long_Text()
	{
		// Act
		string? result = Convert(ClipboardLogConverters.AutomationName, ClipboardEntryFactory.CreateTextEntry(new string('a', 150)));

		// Assert
		result
			.Should()
			.Be(new string('a', 100) + "...");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: an image adds its pixel size to the name of an image without
	/// one.
	/// </summary>
	[Test]
	public void AutomationName_Gives_The_Pixel_Size_Of_An_Image()
	{
		// Arrange
		string? withoutSize = Convert(ClipboardLogConverters.AutomationName, ClipboardEntryFactory.CreateImageEntry([0, 1, 2, 3]));

		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry(PngWithSize(width: 100, height: 50));

		// Act
		string? result = Convert(ClipboardLogConverters.AutomationName, image);

		// Assert
		result
			.Should()
			.Be($"{withoutSize} 100 × 50");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: a link is named by its URL.
	/// </summary>
	[Test]
	public void AutomationName_Gives_The_Url_Of_A_Link()
	{
		// Act
		string? result = Convert(ClipboardLogConverters.AutomationName, ClipboardEntryFactory.CreateUrlEntry("https://example.com"));

		// Assert
		result
			.Should()
			.Be("https://example.com");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: a secret, which the log blurs, carries none of its text in the
	/// name, and every secret gets the same one.
	/// </summary>
	[Test]
	public void AutomationName_Hides_The_Text_Of_A_Secret()
	{
		// Arrange
		ClipboardTextEntry first = ClipboardEntryFactory.CreateTextEntry("Qw7#Lp2!zR9x", isSensitive: true);

		ClipboardTextEntry second = ClipboardEntryFactory.CreateTextEntry("Zx3$Mn8&kT5v", isSensitive: true);

		// Act
		string? firstName = Convert(ClipboardLogConverters.AutomationName, first);

		string? secondName = Convert(ClipboardLogConverters.AutomationName, second);

		// Assert
		firstName
			.Should()
			.NotBeNullOrEmpty()
			.And
			.NotContain(first.Text);

		secondName
			.Should()
			.Be(firstName);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: a text of exactly 100 characters is named whole, without an
	/// ellipsis.
	/// </summary>
	[Test]
	public void AutomationName_Keeps_A_Text_That_Fits_Whole()
	{
		// Act
		string? result = Convert(ClipboardLogConverters.AutomationName, ClipboardEntryFactory.CreateTextEntry(new string('a', 100)));

		// Assert
		result
			.Should()
			.Be(new string('a', 100));
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: a list of files is named by the names of its items.
	/// </summary>
	[Test]
	public void AutomationName_Lists_The_Names_Of_The_Files()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(
		[
			new ClipboardFileSystemEntry(Folder, IsFolder: true),
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false)
		]);

		// Act
		string? result = Convert(ClipboardLogConverters.AutomationName, files);

		// Assert
		result
			.Should()
			.Be($"{FolderName}, a.txt");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.AutomationName" />: a text of several lines is named on one line, each run of
	/// white space turned into one space.
	/// </summary>
	[Test]
	public void AutomationName_Puts_A_Text_On_One_Line()
	{
		// Act
		string? result = Convert(ClipboardLogConverters.AutomationName, ClipboardEntryFactory.CreateTextEntry("  first\r\n\tsecond   third\n"));

		// Assert
		result
			.Should()
			.Be("first second third");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a list longer than the tooltip ends with an ellipsis after as
	/// many files as fit.
	/// </summary>
	[Test]
	public void ContentToolTip_Cuts_A_Very_Long_List_Of_Files()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(ClipboardEntryFactory.CreateFiles(30));

		// Act
		string[] lines = Convert(ClipboardLogConverters.ContentToolTip, files)!.Split(Environment.NewLine);

		// Assert (the counts, 20 files and the ellipsis).
		lines
			.Should()
			.HaveCount(22);

		lines[0]
			.Should()
			.EndWith("Σ: 30");

		lines[^1]
			.Should()
			.Be("...");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a header without a width or a height yields no size.
	/// </summary>
	[TestCase(0, 50)]
	[TestCase(100, 0)]
	public void ContentToolTip_Is_Null_For_A_Png_Without_A_Size(int width, int height)
	{
		// Arrange
		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry(PngWithSize(width, height));

		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, image);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a short list shows no expanded tooltip.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_For_A_Short_List_Of_Files()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(
		[
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false)
		]);

		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, files);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a text shows no tooltip over its entry.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_For_A_Text()
	{
		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, ClipboardEntryFactory.CreateTextEntry("a"));

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: malformed bytes yield no size.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_For_Malformed_Png()
	{
		// Arrange
		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry([0, 1, 2, 3]);

		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, image);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a list the preview shows whole needs no tooltip.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_While_The_Preview_Shows_All_Files()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(ClipboardEntryFactory.CreateFiles(6));

		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, files);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: bytes whose first chunk is not the header yield no size.
	/// </summary>
	[Test]
	public void ContentToolTip_Is_Null_Without_A_Png_Header()
	{
		// Arrange
		byte[] png = PngWithSize(width: 100, height: 50);

		// The first chunk is not "IHDR".
		png[12] = (byte)'X';

		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry(png);

		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, image);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a URL that fills every line of the tooltip is shown whole,
	/// without an ellipsis.
	/// </summary>
	[Test]
	public void ContentToolTip_Keeps_A_Url_That_Fills_The_ToolTip_Whole()
	{
		// Arrange (10 lines of 64 chars).
		string url = "https://example.com/" + new string('a', (64 * 10) - 20);

		ClipboardUrlEntry link = ClipboardEntryFactory.CreateUrlEntry(url);

		// Act
		string[] lines = Convert(ClipboardLogConverters.ContentToolTip, link)!.Split(Environment.NewLine);

		// Assert
		lines
			.Should()
			.HaveCount(10);

		string.Concat(lines)
			.Should()
			.Be(url);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: the size is read from the PNG IHDR header.
	/// </summary>
	[Test]
	public void ContentToolTip_Reports_Png_Pixel_Size()
	{
		// Arrange
		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry(PngWithSize(width: 100, height: 50));

		// Act
		string? result = Convert(ClipboardLogConverters.ContentToolTip, image);

		// Assert
		result
			.Should()
			.Be("100 × 50");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a very long URL is capped with an ellipsis.
	/// </summary>
	[Test]
	public void ContentToolTip_Truncates_Very_Long_Url()
	{
		// Arrange (> 10 lines worth of characters).
		string url = "https://example.com/" + new string('a', 64 * 11);

		ClipboardUrlEntry link = ClipboardEntryFactory.CreateUrlEntry(url);

		// Act
		string[] lines = Convert(ClipboardLogConverters.ContentToolTip, link)!.Split(Environment.NewLine);

		// Assert
		lines
			.Should()
			.HaveCount(10);

		lines[^1]
			.Should()
			.Be("...");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.ContentToolTip" />: a long URL wraps into multiple lines.
	/// </summary>
	[Test]
	public void ContentToolTip_Wraps_Long_Url()
	{
		// Arrange (96 chars -> 2 lines at 64 chars per line).
		string url = "https://example.com/" + new string('a', 76);

		ClipboardUrlEntry link = ClipboardEntryFactory.CreateUrlEntry(url);

		// Act
		string[] lines = Convert(ClipboardLogConverters.ContentToolTip, link)!.Split(Environment.NewLine);

		// Assert
		lines
			.Should()
			.HaveCount(2);

		string.Concat(lines)
			.Should()
			.Be(url);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.FilesPreview" />: a list one file longer than fits keeps the height of the preview,
	/// with an ellipsis as its last line.
	/// </summary>
	[Test]
	public void FilesPreview_Cuts_A_Longer_List_To_The_Same_Height()
	{
		// Arrange
		ClipboardFileSystemEntry[] files = ClipboardEntryFactory.CreateFiles(7);

		// Act
		string[] lines = Convert(ClipboardLogConverters.FilesPreview, files)!.Split(Environment.NewLine);

		// Assert (the counts, five files and the ellipsis).
		lines
			.Should()
			.HaveCount(7);

		lines[^2]
			.Should()
			.EndWith("file4.txt");

		lines[^1]
			.Should()
			.Be("...");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.FilesPreview" />: a short list is shown in full with a header.
	/// </summary>
	[Test]
	public void FilesPreview_Lists_Header_And_All_Items_When_Short()
	{
		// Arrange
		ClipboardFileSystemEntry[] files =
		[
			new ClipboardFileSystemEntry(Folder, IsFolder: true),
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false)
		];

		// Act
		string[] lines = Convert(ClipboardLogConverters.FilesPreview, files)!.Split(Environment.NewLine);

		// Assert (header + 2 items, no ellipsis).
		lines
			.Should()
			.HaveCount(3);

		lines[0]
			.Should()
			.Contain("Σ: 2");

		lines
			.Should()
			.NotContain("...");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.FilesPreview" />: a list that fits is shown to its last file, without an ellipsis.
	/// </summary>
	[Test]
	public void FilesPreview_Shows_A_List_That_Fits_In_Full()
	{
		// Arrange
		ClipboardFileSystemEntry[] files = ClipboardEntryFactory.CreateFiles(6);

		// Act
		string[] lines = Convert(ClipboardLogConverters.FilesPreview, files)!.Split(Environment.NewLine);

		// Assert (the counts and six files).
		lines
			.Should()
			.HaveCount(7);

		lines[^1]
			.Should()
			.EndWith("file5.txt");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.FilesPreview" />: the first line counts the folders, the files and all items, and
	/// each item follows on a line of its own with the glyph of its kind.
	/// </summary>
	[Test]
	public void FilesPreview_Shows_The_Counts_And_A_Glyph_Per_Item()
	{
		// Arrange
		ClipboardFileSystemEntry[] files =
		[
			new ClipboardFileSystemEntry(Folder, IsFolder: true),
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false),
			new ClipboardFileSystemEntry(SecondFile, IsFolder: false)
		];

		// Act
		string[] lines = Convert(ClipboardLogConverters.FilesPreview, files)!.Split(Environment.NewLine);

		// Assert
		lines
			.Should()
			.Equal(
				$"{Glyphs.FileFolder}: 1  {Glyphs.PageFacingUp}: 2  Σ: 3",
				$"{Glyphs.MiddleDot}  {Glyphs.FileFolder}  {FolderName}",
				$"{Glyphs.MiddleDot}  {Glyphs.PageFacingUp}  a.txt",
				$"{Glyphs.MiddleDot}  {Glyphs.PageFacingUp}  b.txt");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.FilesPreview" /> / <see cref="ClipboardLogConverters.ContentToolTip" />:
	/// a long list is truncated in the summary but fully shown (capped) in the tooltip.
	/// </summary>
	[Test]
	public void FilesPreview_Truncates_A_Long_List_And_Exposes_The_ToolTip()
	{
		// Arrange (10 files: more than the 6-item summary budget).
		ClipboardFileSystemEntry[] files = ClipboardEntryFactory.CreateFiles(10);

		// Act
		string[] lines = Convert(ClipboardLogConverters.FilesPreview, files)!.Split(Environment.NewLine);

		string? toolTip = Convert(ClipboardLogConverters.ContentToolTip, ClipboardEntryFactory.CreateFilesEntry(files));

		// Assert
		lines[^1]
			.Should()
			.Be("...");

		toolTip
			.Should()
			.NotBeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.PngToPreview" />: the image is decoded at the width given as the parameter, whatever its own
	/// size.
	/// </summary>
	[AvaloniaTest]
	public void PngToPreview_Decodes_The_Png_At_The_Width_Given()
	{
		// Act
		Bitmap? result = (Bitmap?)Convert(ClipboardLogConverters.PngToPreview, ImageFactory.CreatePng(), 48.0);

		// Assert
		result!.PixelSize.Width
			.Should()
			.Be(48);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.PngToPreview" />: empty bytes give no image.
	/// </summary>
	[Test]
	public void PngToPreview_Without_A_Png_Is_Empty()
	{
		// Act
		object? result = Convert(ClipboardLogConverters.PngToPreview, [], 48.0);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeGlyph" />: the badge marks a list of files.
	/// </summary>
	[Test]
	public void TypeGlyph_Is_Files_Specific()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry();

		// Act
		string? result = Convert(ClipboardLogConverters.TypeGlyph, files);

		// Assert
		result
			.Should()
			.Be(Glyphs.CardIndexDividers);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeGlyph" />: the badge marks an image.
	/// </summary>
	[Test]
	public void TypeGlyph_Is_Image_Specific()
	{
		// Arrange
		ClipboardImageEntry image = ClipboardEntryFactory.CreateImageEntry([]);

		// Act
		string? result = Convert(ClipboardLogConverters.TypeGlyph, image);

		// Assert
		result
			.Should()
			.Be(Glyphs.FramedPicture);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeGlyph" />: the badge marks a link.
	/// </summary>
	[Test]
	public void TypeGlyph_Is_Url_Specific()
	{
		// Arrange
		ClipboardUrlEntry link = ClipboardEntryFactory.CreateUrlEntry("https://example.com");

		// Act
		string? result = Convert(ClipboardLogConverters.TypeGlyph, link);

		// Assert
		result
			.Should()
			.Be(Glyphs.Link);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeGlyph" /> across format combinations.
	/// </summary>
	[TestCase(null, null, Glyphs.InputLatinLetters)]
	[TestCase("<b>a</b>", null, Glyphs.AngleBracketSlash)]
	[TestCase(null, @"{\rtf1 a}", Glyphs.BButton)]
	[TestCase("<b>a</b>", @"{\rtf1 a}", $"{Glyphs.AngleBracketSlash} {Glyphs.BButton}")]
	public void TypeGlyph_Reflects_Format_Combination(string? html, string? rtf, string expected)
	{
		// Arrange
		ClipboardTextEntry text = ClipboardEntryFactory.CreateTextEntry("a", html: html, rtf: rtf);

		// Act
		string? result = Convert(ClipboardLogConverters.TypeGlyph, text);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeToolTip" />: every kind of entry names itself in its own way.
	/// </summary>
	[Test]
	public void TypeToolTip_Differs_Between_Entry_Kinds()
	{
		// Arrange
		ClipboardLogEntryBase[] entries =
		[
			ClipboardEntryFactory.CreateTextEntry("a"),
			ClipboardEntryFactory.CreateUrlEntry("https://example.com"),
			ClipboardEntryFactory.CreateImageEntry([]),
			ClipboardEntryFactory.CreateFilesEntry()
		];

		// Act
		string?[] tooltips = [.. entries.Select(static x => Convert(ClipboardLogConverters.TypeToolTip, x))];

		// Assert
		tooltips
			.Should()
			.NotContainNulls()
			.And
			.OnlyHaveUniqueItems();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeToolTip" />: every combination of the companion formats of a text names itself
	/// in its own way.
	/// </summary>
	[Test]
	public void TypeToolTip_Differs_Between_Format_Combinations()
	{
		// Arrange
		ClipboardTextEntry[] entries =
		[
			ClipboardEntryFactory.CreateTextEntry("a"),
			ClipboardEntryFactory.CreateTextEntry("a", html: "<b>a</b>"),
			ClipboardEntryFactory.CreateTextEntry("a", rtf: @"{\rtf1 a}"),
			ClipboardEntryFactory.CreateTextEntry("a", html: "<b>a</b>", rtf: @"{\rtf1 a}")
		];

		// Act
		string?[] tooltips = [.. entries.Select(static x => Convert(ClipboardLogConverters.TypeToolTip, x))];

		// Assert
		tooltips
			.Should()
			.NotContainNulls()
			.And
			.OnlyHaveUniqueItems();
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeToolTip" />: a text with both companion formats names both, HTML first.
	/// </summary>
	[Test]
	public void TypeToolTip_Names_Both_Formats_Of_A_Text_With_Both()
	{
		// Arrange
		ClipboardTextEntry withHtml = ClipboardEntryFactory.CreateTextEntry("a", html: "<b>a</b>");

		ClipboardTextEntry withRtf = ClipboardEntryFactory.CreateTextEntry("a", rtf: @"{\rtf1 a}");

		ClipboardTextEntry withBoth = ClipboardEntryFactory.CreateTextEntry("a", html: "<b>a</b>", rtf: @"{\rtf1 a}");

		string? html = Convert(ClipboardLogConverters.TypeToolTip, withHtml);

		string? rtf = Convert(ClipboardLogConverters.TypeToolTip, withRtf);

		// Act
		string? result = Convert(ClipboardLogConverters.TypeToolTip, withBoth);

		// Assert
		result
			.Should()
			.Be($"{html} + {rtf}");
	}

	/// <summary>
	/// <see cref="ClipboardLogConverters.TypeToolTip" />: folder / file / total counts are reported.
	/// </summary>
	[Test]
	public void TypeToolTip_Reports_Folder_And_File_Counts()
	{
		// Arrange
		ClipboardFilesEntry files = ClipboardEntryFactory.CreateFilesEntry(
		[
			new ClipboardFileSystemEntry(Folder, IsFolder: true),
			new ClipboardFileSystemEntry(FirstFile, IsFolder: false),
			new ClipboardFileSystemEntry(SecondFile, IsFolder: false)
		]);

		// Act
		string[] lines = Convert(ClipboardLogConverters.TypeToolTip, files)!.Split(Environment.NewLine);

		// Assert (a line per kind: folders, files, and the total).
		lines
			.Should()
			.HaveCount(3);

		lines[0]
			.Should()
			.EndWith("1");

		lines[1]
			.Should()
			.EndWith("2");

		lines[2]
			.Should()
			.Be("Σ: 3");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over an entry of the clipboard log.
	/// </summary>
	private static string? Convert(
		FuncValueConverter<ClipboardLogEntryBase?, string?> converter,
		ClipboardLogEntryBase entry)
	{
		return (string?)converter.Convert(
			entry,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over a list of files.
	/// </summary>
	private static string? Convert(
		FuncValueConverter<IReadOnlyList<ClipboardFileSystemEntry>?, string?> converter,
		IReadOnlyList<ClipboardFileSystemEntry> files)
	{
		return (string?)converter.Convert(
			files,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}

	/// <summary>
	/// Runs a converter over the bytes of a PNG with the width given as the parameter.
	/// </summary>
	private static object? Convert(FuncValueConverter<byte[]?, double, Bitmap?> converter, byte[]? png, double width)
	{
		return converter.Convert(
			png,
			typeof(object),
			width,
			CultureInfo.CurrentCulture);
	}

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
