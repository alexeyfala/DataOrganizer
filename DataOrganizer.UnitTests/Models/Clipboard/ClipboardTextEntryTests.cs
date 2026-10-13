using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardTextEntry)}"" type")]
internal class ClipboardTextEntryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardTextEntry.TypeGlyph" /> across format combinations.
	/// </summary>
	[Test]
	public void TypeGlyph_Reflects_Format_Combination()
	{
		// Arrange, Act, Assert
		ClipboardEntryFactory.CreateTextEntry("a").TypeGlyph
			.Should()
			.Be(Glyphs.InputLatinLetters);

		ClipboardEntryFactory.CreateTextEntry("a", html: "<b>a</b>").TypeGlyph
			.Should()
			.Be(Glyphs.AngleBracketSlash);

		ClipboardEntryFactory.CreateTextEntry("a", rtf: @"{\rtf1 a}").TypeGlyph
			.Should()
			.Be(Glyphs.BButton);

		ClipboardEntryFactory.CreateTextEntry("a", html: "<b>a</b>", rtf: @"{\rtf1 a}").TypeGlyph
			.Should()
			.Be($"{Glyphs.AngleBracketSlash} {Glyphs.BButton}");
	}
	#endregion
}
