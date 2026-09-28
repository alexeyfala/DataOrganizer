using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(IndentFoldingStrategy)}"" type")]
internal partial class IndentFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Columns from one tab stop to the next.
	/// </summary>
	private const int TabSize = 4;

	/// <summary>
	/// Rules of a language with the block markers of JavaScript.
	/// </summary>
	private static readonly SyntaxFoldingRules MarkedRules = new()
	{
		EndMarker = EndMarkerRegex(),
		StartMarker = StartMarkerRegex()
	};

	/// <summary>
	/// Rules of a language of the off-side rule without markers.
	/// </summary>
	private static readonly SyntaxFoldingRules OffSideRules = new()
	{
		IsOffSide = true
	};

	/// <summary>
	/// Rules of a language without markers.
	/// </summary>
	private static readonly SyntaxFoldingRules PlainRules = new();
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a tab reaches the next tab stop, in line with the spaces up to it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Counts_A_Tab_To_The_Next_Tab_Stop()
	{
		// Arrange
		TextDocument document = new("a\n  \tb\n    c");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		// Two spaces and a tab reach the column of four spaces, so the two lines make one block.
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: the blank lines after a block fold with it, while under
	/// the off-side rule they belong to the next block.
	/// </summary>
	[Test]
	[TestCase(false, 3)]
	[TestCase(true, 2)]
	public void CreateNewFoldings_Ends_A_Block_At_The_Blank_Lines_By_The_Off_Side_Rule(bool isOffSide, int end)
	{
		// Arrange
		TextDocument document = new("a\n    b\n\nc");

		SyntaxFoldingRules rules = new()
		{
			IsOffSide = isOffSide
		};

		IndentFoldingStrategy sut = new(rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, end));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a marked block folds with the line of its end marker.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Marked_Block_With_Its_End_Line()
	{
		// Arrange
		TextDocument document = new("""
			// #region A
			x
			// #endregion
			y
			""");

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block folds from the end of its first line, which stays
	/// in view, to the end of its last line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_From_The_End_Of_The_First_Line_To_The_End_Of_The_Last()
	{
		// Arrange
		TextDocument document = new("a\n    b\n    c\nd");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((document.GetLineByNumber(1).EndOffset, document.GetLineByNumber(3).EndOffset));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: an empty document has no blocks.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_Nothing_In_An_Empty_Document()
	{
		// Arrange
		TextDocument document = new(string.Empty);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: lines of one indentation make no blocks.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_Nothing_Without_Indentation()
	{
		// Arrange
		TextDocument document = new("a\nb\nc");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: the lines inside a marked block fold by indentation too.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_The_Blocks_Inside_A_Marked_Block()
	{
		// Arrange
		TextDocument document = new("""
			// #region A
			x
			    y
			// #endregion
			""");

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4), (2, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: the deeper lines under a line make up its block.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_The_Deeper_Lines_Under_A_Line()
	{
		// Arrange
		TextDocument document = new("a\n    b\n    c\nd");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a closing bracket with more text on its line stays out of
	/// the block before it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Closing_Bracket_With_More_Text_Out()
	{
		// Arrange
		TextDocument document = new("""
			if (a) {
			    x();
			} else {
			    y();
			}
			""");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 5));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a lone opening bracket after a finished line, such as the
	/// end of an item, starts its block itself.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Lone_Bracket_After_A_Finished_Line()
	{
		// Arrange
		TextDocument document = new("""
			[
			  {
			    "a": 1
			  },
			  {
			    "b": 2
			  }
			]
			""");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 8), (2, 4), (5, 7));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a lone opening bracket under a marker starts its block
	/// itself, as the marker heads a block of its own.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Lone_Bracket_Under_A_Marker()
	{
		// Arrange
		TextDocument document = new("""
			// #region A
			{
			    x
			}
			// #endregion
			""");

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5), (2, 4));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block inside a block folds on its own, and the blocks
	/// come in the order of their first lines.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Nests_The_Blocks()
	{
		// Arrange
		TextDocument document = new("a\n    b\n        c\n    d\ne");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4), (2, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: under the off-side rule the brackets move no block.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Off_Side_Leaves_The_Brackets_Alone()
	{
		// Arrange
		TextDocument document = new("""
			a
			{
			    b
			}
			""");

		IndentFoldingStrategy sut = new(OffSideRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((2, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: indentation reads any text, so there is no error to report.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_No_Error()
	{
		// Arrange
		TextDocument document = new("a\n    b");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		sut.CreateNewFoldings(document, out int firstErrorOffset);

		// Assert
		firstErrorOffset
			.Should()
			.Be(-1);
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: the block of a lone opening bracket starts on the line
	/// above it and takes in the line of the closing bracket.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Starts_A_Block_Of_A_Lone_Bracket_On_The_Line_Above()
	{
		// Arrange
		TextDocument document = new("""
			void F()
			{
			    x();
			}
			""");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: the blocks of lone brackets inside each other start on the
	/// lines above their brackets.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Starts_The_Nested_Blocks_Of_Lone_Brackets_On_The_Lines_Above()
	{
		// Arrange
		TextDocument document = new("""
			class C
			{
			    void F()
			    {
			        x();
			    }
			}
			""");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 7), (3, 6));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a start marker without an end marker below it is an
	/// ordinary line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_A_Start_Marker_Without_An_End_For_An_Ordinary_Line()
	{
		// Arrange
		TextDocument document = new("""
			// #region A
			x
			y
			""");

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block that opens a bracket at the end of its first line
	/// takes in the line of the closing bracket.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_The_Closing_Bracket_Into_A_Block()
	{
		// Arrange
		TextDocument document = new("""
			void F() {
			    x();
			}
			y
			""");

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the line that closes a block marked in the way of JavaScript.
	/// </summary>
	[GeneratedRegex(@"^\s*//\s*#endregion\b")]
	private static partial Regex EndMarkerRegex();

	/// <summary>
	/// Returns the numbers of the first and the last lines of the blocks.
	/// </summary>
	private static (int Start, int End)[] GetLines(TextDocument document, IEnumerable<NewFolding> foldings)
	{
		return [.. foldings.Select(x => (
			document.GetLineByOffset(x.StartOffset).LineNumber,
			document.GetLineByOffset(x.EndOffset).LineNumber))];
	}

	/// <summary>
	/// Matches the line that opens a block marked in the way of JavaScript.
	/// </summary>
	[GeneratedRegex(@"^\s*//\s*#region\b")]
	private static partial Regex StartMarkerRegex();
	#endregion
}
