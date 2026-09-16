using AwesomeAssertions;
using DataOrganizer.Helpers;
using DataOrganizer.Models.Clipboard;
using DataOrganizer.UnitTests.Factories;
using Shared.Properties;

namespace DataOrganizer.UnitTests.Models.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardTextEntry)}"" type")]
internal class ClipboardTextEntryTests
{
	#region Methods
	/// <summary>
	/// <see cref="ClipboardTextEntry.IsHtml" />, <see cref="ClipboardTextEntry.IsRtf" />: plain text carries no companion format.
	/// </summary>
	[Test]
	public void IsHtml_And_IsRtf_Are_False_For_Plain_Text()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("plain");

		// Act, Assert
		sut.IsHtml
			.Should()
			.BeFalse();

		sut.IsRtf
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardTextEntry.IsHtml" />, <see cref="ClipboardTextEntry.IsRtf" />: both companion formats are reported.
	/// </summary>
	[Test]
	public void IsHtml_And_IsRtf_Reflect_Companion_Formats()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("x", html: "<b>x</b>", rtf: @"{\rtf1 x}");

		// Act, Assert
		sut.IsHtml
			.Should()
			.BeTrue();

		sut.IsRtf
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="ClipboardTextEntry.IsSensitive" />: ordinary prose is not flagged.
	/// </summary>
	[Test]
	public void IsSensitive_Is_False_For_Plain_Prose()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("hello world");

		// Act, Assert
		sut.IsSensitive
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="ClipboardTextEntry.IsSensitive" />: a high-entropy token is flagged.
	/// </summary>
	[Test]
	public void IsSensitive_Is_True_For_Secret_Like_Token()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("Xy7$kQ9pLm2!");

		// Act, Assert
		sut.IsSensitive
			.Should()
			.BeTrue();
	}

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

	/// <summary>
	/// <see cref="ClipboardTextEntry.TypeToolTip" /> for a plain-text entry.
	/// </summary>
	[Test]
	public void TypeToolTip_Is_PlainText_For_Plain_Text()
	{
		// Arrange
		ClipboardTextEntry sut = ClipboardEntryFactory.CreateTextEntry("a");

		// Act, Assert
		sut.TypeToolTip
			.Should()
			.Be(Strings.PlainText);
	}
	#endregion
}
