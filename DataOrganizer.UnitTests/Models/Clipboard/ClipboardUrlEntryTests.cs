using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using System;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardUrlEntry)}"" type")]
internal class ClipboardUrlEntryTests
{
	#region Methods
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

	/// <summary>
	/// <see cref="ClipboardUrlEntry.IsUrl" />, <see cref="ClipboardUrlEntry.TypeGlyph" />: the badge marks a link.
	/// </summary>
	[Test]
	public void IsUrl_And_TypeGlyph_Are_Url_Specific()
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
	}
	#endregion
}
