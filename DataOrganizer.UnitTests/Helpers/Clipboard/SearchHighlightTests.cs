using AwesomeAssertions;
using DataOrganizer.Dto.Clipboard;
using DataOrganizer.Helpers.Clipboard;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.Helpers.Clipboard;

[TestFixture(Description = $@"Tests of ""{nameof(SearchHighlight)}"" type")]
internal class SearchHighlightTests
{
	#region Methods
	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: a blank query yields a single plain segment.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	public void SplitSegments_Blank_Query_Yields_Single_Plain_Segment(string? query)
	{
		// Act
		IReadOnlyList<SearchHighlightSegment> result = SearchHighlight.SplitSegments("hello world", query);

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(new SearchHighlightSegment("hello world", IsMatch: false));
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: empty text yields no segments.
	/// </summary>
	[Test]
	public void SplitSegments_Empty_Text_Yields_Nothing()
	{
		// Act
		IReadOnlyList<SearchHighlightSegment> result = SearchHighlight.SplitSegments(string.Empty, "x");

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: every occurrence is flagged, with plain text between.
	/// </summary>
	[Test]
	public void SplitSegments_Flags_Every_Occurrence()
	{
		// Act
		IReadOnlyList<SearchHighlightSegment> result = SearchHighlight.SplitSegments("a b a b a", "a");

		// Assert
		result
			.Should()
			.Equal(
				new SearchHighlightSegment("a", IsMatch: true),
				new SearchHighlightSegment(" b ", IsMatch: false),
				new SearchHighlightSegment("a", IsMatch: true),
				new SearchHighlightSegment(" b ", IsMatch: false),
				new SearchHighlightSegment("a", IsMatch: true));
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: matching is case-insensitive.
	/// </summary>
	[Test]
	public void SplitSegments_Matches_Case_Insensitively()
	{
		// Act
		IReadOnlyList<SearchHighlightSegment> result = SearchHighlight.SplitSegments("The Apple", "apple");

		// Assert
		result
			.Should()
			.Equal(
				new SearchHighlightSegment("The ", IsMatch: false),
				new SearchHighlightSegment("Apple", IsMatch: true));
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: an unmatched query yields a single plain segment.
	/// </summary>
	[Test]
	public void SplitSegments_No_Match_Yields_Single_Plain_Segment()
	{
		// Act
		IReadOnlyList<SearchHighlightSegment> result = SearchHighlight.SplitSegments("hello world", "zzz");

		// Assert
		result
			.Should()
			.Equal(new SearchHighlightSegment("hello world", IsMatch: false));
	}
	#endregion
}
