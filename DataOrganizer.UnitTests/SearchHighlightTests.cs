using AwesomeAssertions;
using DataOrganizer.Helpers.Clipboard;
using System.Collections.Generic;

namespace DataOrganizer.UnitTests.TestTypes;

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
		IReadOnlyList<SearchHighlight.Segment> result = SearchHighlight.SplitSegments("hello world", query);

		// Assert
		result
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.Be(new SearchHighlight.Segment("hello world", IsMatch: false));
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: empty text yields no segments.
	/// </summary>
	[Test]
	public void SplitSegments_Empty_Text_Yields_Nothing()
	{
		// Act
		IReadOnlyList<SearchHighlight.Segment> result = SearchHighlight.SplitSegments(string.Empty, "x");

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
		IReadOnlyList<SearchHighlight.Segment> result = SearchHighlight.SplitSegments("a b a b a", "a");

		// Assert
		result
			.Should()
			.Equal(
				new SearchHighlight.Segment("a", IsMatch: true),
				new SearchHighlight.Segment(" b ", IsMatch: false),
				new SearchHighlight.Segment("a", IsMatch: true),
				new SearchHighlight.Segment(" b ", IsMatch: false),
				new SearchHighlight.Segment("a", IsMatch: true));
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: matching is case-insensitive.
	/// </summary>
	[Test]
	public void SplitSegments_Matches_Case_Insensitively()
	{
		// Act
		IReadOnlyList<SearchHighlight.Segment> result = SearchHighlight.SplitSegments("The Apple", "apple");

		// Assert
		result
			.Should()
			.Equal(
				new SearchHighlight.Segment("The ", IsMatch: false),
				new SearchHighlight.Segment("Apple", IsMatch: true));
	}

	/// <summary>
	/// <see cref="SearchHighlight.SplitSegments" />: an unmatched query yields a single plain segment.
	/// </summary>
	[Test]
	public void SplitSegments_No_Match_Yields_Single_Plain_Segment()
	{
		// Act
		IReadOnlyList<SearchHighlight.Segment> result = SearchHighlight.SplitSegments("hello world", "zzz");

		// Assert
		result
			.Should()
			.Equal(new SearchHighlight.Segment("hello world", IsMatch: false));
	}
	#endregion
}
