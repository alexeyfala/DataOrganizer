using AwesomeAssertions;
using DataOrganizer.DTO.Clipboard;
using Shared.Properties;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(ClipboardTextEntry)}"" type")]
internal class ClipboardTextEntryTests
{
	#region Methods
	/// <summary>
	/// Test of the format flags for a plain-text entry.
	/// </summary>
	[Test]
	public void Flags_Are_False_For_Plain_Text()
	{
		// Arrange
		ClipboardTextEntry sut = TextEntry("plain");

		// Act, Assert
		sut.IsHtml
			.Should()
			.BeFalse();

		sut.IsRtf
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// Test of the format flags when both companion formats are present.
	/// </summary>
	[Test]
	public void Flags_Reflect_Companion_Formats()
	{
		// Arrange
		ClipboardTextEntry sut = TextEntry("x", html: "<b>x</b>", rtf: @"{\rtf1 x}");

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
		ClipboardTextEntry sut = TextEntry("hello world");

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
		ClipboardTextEntry sut = TextEntry("Xy7$kQ9pLm2!");

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
		TextEntry("a").TypeGlyph
			.Should()
			.Be("🔤");

		TextEntry("a", html: "<b>a</b>").TypeGlyph
			.Should()
			.Be("</>");

		TextEntry("a", rtf: @"{\rtf1 a}").TypeGlyph
			.Should()
			.Be("🅱️");

		TextEntry("a", html: "<b>a</b>", rtf: @"{\rtf1 a}").TypeGlyph
			.Should()
			.Be("</> 🅱️");
	}

	/// <summary>
	/// <see cref="ClipboardTextEntry.TypeToolTip" /> for a plain-text entry.
	/// </summary>
	[Test]
	public void TypeToolTip_Is_PlainText_For_Plain_Text()
	{
		// Arrange
		ClipboardTextEntry sut = TextEntry("a");

		// Act, Assert
		sut.TypeToolTip
			.Should()
			.Be(Strings.PlainText);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// A text entry with optional companion formats.
	/// </summary>
	private static ClipboardTextEntry TextEntry(
		string text,
		string? html = null,
		string? rtf = null) => new()
		{
			Text = text,
			Html = html,
			Rtf = rtf,
			Hash = [1]
		};
	#endregion
}
