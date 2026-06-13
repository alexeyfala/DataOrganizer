using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SearchSnippet)}"" type")]
internal class SearchSnippetTests
{
	#region Data
	/// <summary>
	/// Horizontal ellipsis the excerpt uses to mark trimmed sides.
	/// </summary>
	private const string Ellipsis = "…";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SearchSnippet.Build" />: a blank query returns the whole collapsed text.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	public void Build_Blank_Query_Returns_Collapsed_Text(string? query)
	{
		// Act
		string result = SearchSnippet.Build("  hello   world \n foo ", query);

		// Assert
		result
			.Should()
			.Be("hello world foo");
	}

	/// <summary>
	/// <see cref="SearchSnippet.Build" />: runs of whitespace (spaces, tabs, newlines) collapse to a
	/// single space and the ends are trimmed.
	/// </summary>
	[Test]
	public void Build_Collapses_Whitespace()
	{
		// Act
		string result = SearchSnippet.Build("a\t\tb\r\nc   d", "b");

		// Assert
		result
			.Should()
			.Be("a b c d");
	}

	/// <summary>
	/// <see cref="SearchSnippet.Build" />: the whole match is preserved even when it is longer than
	/// the nominal window.
	/// </summary>
	[Test]
	public void Build_Keeps_Whole_Match_When_Longer_Than_Window()
	{
		// Arrange
		string match = new('Q', 300);

		string text = "ab" + match;

		// Act
		string result = SearchSnippet.Build(text, match, leadingContext: 0, windowLength: 10);

		// Assert
		result
			.Should()
			.Contain(match);
	}

	/// <summary>
	/// <see cref="SearchSnippet.Build" />: a match deep in a long text is excerpted with ellipses on
	/// both sides while still containing the match.
	/// </summary>
	[Test]
	public void Build_Match_In_Long_Text_Is_Excerpted_On_Both_Sides()
	{
		// Arrange
		string text = new string('x', 50) + "MATCH" + new string('y', 300);

		// Act
		string result = SearchSnippet.Build(text, "MATCH");

		// Assert
		result
			.Should()
			.StartWith(Ellipsis);

		result
			.Should()
			.EndWith(Ellipsis);

		result
			.Should()
			.Contain("MATCH");
	}

	/// <summary>
	/// <see cref="SearchSnippet.Build" />: a match within the leading context window keeps the start
	/// of the text, so the excerpt is not prefixed with an ellipsis.
	/// </summary>
	[Test]
	public void Build_Match_Near_Start_Has_No_Leading_Ellipsis()
	{
		// Act
		string result = SearchSnippet.Build("Application start", "App");

		// Assert
		result
			.Should()
			.Be("Application start");

		result
			.Should()
			.NotStartWith(Ellipsis);
	}

	/// <summary>
	/// <see cref="SearchSnippet.Build" />: matching is case-insensitive.
	/// </summary>
	[Test]
	public void Build_Matches_Case_Insensitively()
	{
		// Act
		string result = SearchSnippet.Build("The Apple", "apple");

		// Assert
		result
			.Should()
			.Be("The Apple");
	}

	/// <summary>
	/// <see cref="SearchSnippet.Build" />: a query that is absent returns the whole collapsed text.
	/// </summary>
	[Test]
	public void Build_Query_Not_Found_Returns_Collapsed_Text()
	{
		// Act
		string result = SearchSnippet.Build("hello world", "zzz");

		// Assert
		result
			.Should()
			.Be("hello world");
	}
	#endregion
}
