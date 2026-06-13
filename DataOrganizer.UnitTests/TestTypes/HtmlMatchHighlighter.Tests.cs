using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(HtmlMatchHighlighter)}"" type")]
internal partial class HtmlMatchHighlighterTests
{
	#region Data
	/// <summary>
	/// Id assigned to the first match.
	/// </summary>
	private const string MatchId = "m";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="HtmlMatchHighlighter.Highlight" />: a query that occurs only inside a tag or attribute
	/// is not highlighted (only text nodes are touched).
	/// </summary>
	[Test]
	public void Highlight_Ignores_Tags_And_Attributes()
	{
		// Act
		string result = HtmlMatchHighlighter.Highlight("<p class=\"apple\">fruit</p>", "apple", MatchId);

		// Assert
		result
			.Should()
			.NotContain("<span")
			.And
			.Contain("class=\"apple\"");
	}

	/// <summary>
	/// <see cref="HtmlMatchHighlighter.Highlight" />: matching is case-insensitive and the match keeps
	/// its original casing.
	/// </summary>
	[Test]
	public void Highlight_Is_Case_Insensitive_And_Preserves_Casing()
	{
		// Act
		string result = HtmlMatchHighlighter.Highlight("<p>An Apple</p>", "apple", MatchId);

		// Assert
		result
			.Should()
			.Contain(">Apple</span>");
	}

	/// <summary>
	/// <see cref="HtmlMatchHighlighter.Highlight" />: an absent or blank query returns the input unchanged.
	/// </summary>
	[TestCase("zzz")]
	[TestCase("")]
	[TestCase("   ")]
	public void Highlight_No_Match_Returns_Input(string query)
	{
		// Arrange
		const string html = "<p>hello</p>";

		// Act
		string result = HtmlMatchHighlighter.Highlight(html, query, MatchId);

		// Assert
		result
			.Should()
			.Be(html);
	}

	/// <summary>
	/// <see cref="HtmlMatchHighlighter.Highlight" />: text inside a style block is never highlighted.
	/// </summary>
	[Test]
	public void Highlight_Skips_Style_Blocks()
	{
		// Act
		string result = HtmlMatchHighlighter.Highlight("<style>p { color: red; }</style><p>red</p>", "red", MatchId);

		// Assert
		SpanTagRegex()
			.Count(result)
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="HtmlMatchHighlighter.Highlight" />: every occurrence is wrapped, but only the first
	/// match gets the id.
	/// </summary>
	[Test]
	public void Highlight_Wraps_All_Occurrences_But_Only_First_Has_Id()
	{
		// Act
		string result = HtmlMatchHighlighter.Highlight("<p>aXaXa</p>", "a", MatchId);

		// Assert
		SpanTagRegex()
			.Count(result)
			.Should()
			.Be(3);

		MatchIdRegex()
			.Count(result)
			.Should()
			.Be(1);
	}

	/// <summary>
	/// <see cref="HtmlMatchHighlighter.Highlight" />: a match is wrapped in a styled span and the first
	/// one carries the id.
	/// </summary>
	[Test]
	public void Highlight_Wraps_Match_With_Styled_Span_And_Id()
	{
		// Act
		string result = HtmlMatchHighlighter.Highlight("<p>Hello world</p>", "world", MatchId);

		// Assert
		result
			.Should()
			.Contain("background-color:#FFE08A")
			.And
			.Contain($"id=\"{MatchId}\"")
			.And
			.Contain(">world</span>");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the id attribute assigned to the first highlighted match (see <see cref="MatchId" />).
	/// </summary>
	[GeneratedRegex("id=\"m\"")]
	private static partial Regex MatchIdRegex();

	/// <summary>
	/// Matches the opening tag of each highlight span.
	/// </summary>
	[GeneratedRegex("<span")]
	private static partial Regex SpanTagRegex();
	#endregion
}
