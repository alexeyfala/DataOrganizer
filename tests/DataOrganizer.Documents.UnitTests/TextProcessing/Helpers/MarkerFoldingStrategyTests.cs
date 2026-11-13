using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Documents.TextProcessing.Dto;
using DataOrganizer.Documents.TextProcessing.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.Documents.UnitTests.TextProcessing.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(MarkerFoldingStrategy)}"" type")]
internal partial class MarkerFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Rules of a language with the region markers of XML.
	/// </summary>
	private static readonly SyntaxFoldingRules Rules = new()
	{
		EndMarker = EndMarkerRegex(),
		StartMarker = StartMarkerRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="MarkerFoldingStrategy.CreateNewFoldings" />: a block runs from the end of the line of its start marker to
	/// the end of the line of its end marker, so the start marker stays in view.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Block_Between_Its_Markers()
	{
		// Arrange
		TextDocument document = new("<!-- #region A -->\n<a />\n<!-- #endregion -->");

		using FoldingText text = new(document.Text);

		MarkerFoldingStrategy sut = new(Rules);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((document.GetLineByNumber(1).EndOffset, document.TextLength));
	}

	/// <summary>
	/// <see cref="MarkerFoldingStrategy.CreateNewFoldings" />: indented lines without markers make no block.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_No_Block_By_Indentation()
	{
		// Arrange
		using FoldingText text = new("<a>\n  <b />\n  <c />\n</a>");

		MarkerFoldingStrategy sut = new(Rules);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkerFoldingStrategy.CreateNewFoldings" />: a language without markers marks no blocks.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_No_Block_Without_Markers()
	{
		// Arrange
		using FoldingText text = new("<!-- #region A -->\n<a />\n<!-- #endregion -->");

		MarkerFoldingStrategy sut = new(new SyntaxFoldingRules());

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkerFoldingStrategy.CreateNewFoldings" />: an end marker closes the nearest start marker above it, so
	/// blocks nest.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_The_Blocks_That_Nest()
	{
		// Arrange
		TextDocument document = new("<!-- #region A -->\n<!-- #region B -->\n<a />\n<!-- #endregion -->\n<!-- #endregion -->");

		using FoldingText text = new(document.Text);

		MarkerFoldingStrategy sut = new(Rules);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings).Order()
			.Should()
			.Equal((1, 5), (2, 4));
	}

	/// <summary>
	/// <see cref="MarkerFoldingStrategy.CreateNewFoldings" />: a start marker without an end below it is an ordinary line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Skips_A_Start_Without_An_End()
	{
		// Arrange
		TextDocument document = new("<!-- #region A -->\n<!-- #region B -->\n<a />\n<!-- #endregion -->");

		using FoldingText text = new(document.Text);

		MarkerFoldingStrategy sut = new(Rules);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((2, 4));
	}

	/// <summary>
	/// <see cref="MarkerFoldingStrategy.CreateNewFoldings" />: an end marker without a start above it is an ordinary line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Skips_An_End_Without_A_Start()
	{
		// Arrange
		TextDocument document = new("<!-- #endregion -->\n<!-- #region A -->\n<a />\n<!-- #endregion -->");

		using FoldingText text = new(document.Text);

		MarkerFoldingStrategy sut = new(Rules);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((2, 4));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the line that closes a region of XML.
	/// </summary>
	[GeneratedRegex(@"^\s*<!--\s*#endregion\b.*-->")]
	private static partial Regex EndMarkerRegex();

	/// <summary>
	/// Returns the numbers of the first and the last lines of the blocks.
	/// </summary>
	private static (int Start, int End)[] GetLines(TextDocument document, IEnumerable<TextFolding> foldings)
	{
		return [.. foldings.Select(x => (
			document.GetLineByOffset(x.StartOffset).LineNumber,
			document.GetLineByOffset(x.EndOffset).LineNumber))];
	}

	/// <summary>
	/// Matches the line that opens a region of XML.
	/// </summary>
	[GeneratedRegex(@"^\s*<!--\s*#region\b.*-->")]
	private static partial Regex StartMarkerRegex();
	#endregion
}
