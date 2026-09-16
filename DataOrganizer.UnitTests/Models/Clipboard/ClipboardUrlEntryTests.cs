using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using Shared.Properties;
using System;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardUrlEntry)}"" type")]
internal class ClipboardUrlEntryTests
{
	#region Methods
	/// <summary>
	/// Test of the URL-specific badge metadata.
	/// </summary>
	[Test]
	public void Badge_Metadata_Is_Url_Specific()
	{
		// Arrange
		ClipboardUrlEntry sut = ClipboardEntryFactory.CreateUrlEntry("https://example.com");

		// Act, Assert
		sut.IsUrl
			.Should()
			.BeTrue();

		sut.TypeGlyph
			.Should()
			.Be(Glyphs.Link);

		sut.TypeToolTip
			.Should()
			.Be(Strings.Hyperlink);
	}

	/// <summary>
	/// <see cref="ClipboardUrlEntry.ContentToolTip" />: a very long URL is capped with an ellipsis.
	/// </summary>
	[Test]
	public void ContentToolTip_Truncates_Very_Long_Url()
	{
		// Arrange (> 10 lines worth of characters).
		string url = "https://example.com/" + new string('a', 64 * 11);

		ClipboardUrlEntry sut = ClipboardEntryFactory.CreateUrlEntry(url);

		// Act
		string[] lines = sut
			.ContentToolTip!
			.Split(Environment.NewLine);

		// Assert
		lines
			.Should()
			.HaveCount(10);

		lines[^1]
			.Should()
			.Be("...");
	}

	/// <summary>
	/// <see cref="ClipboardUrlEntry.ContentToolTip" />: a long URL wraps into multiple lines.
	/// </summary>
	[Test]
	public void ContentToolTip_Wraps_Long_Url()
	{
		// Arrange (96 chars -> 2 lines at 64 chars per line).
		string url = "https://example.com/" + new string('a', 76);

		ClipboardUrlEntry sut = ClipboardEntryFactory.CreateUrlEntry(url);

		// Act
		string[] lines = sut
			.ContentToolTip!
			.Split(Environment.NewLine);

		// Assert
		lines
			.Should()
			.HaveCount(2);

		string.Concat(lines)
			.Should()
			.Be(url);
	}
	#endregion
}
