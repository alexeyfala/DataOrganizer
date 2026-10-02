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
	/// Rules of a language with the directives and the block markers of C#.
	/// </summary>
	private static readonly SyntaxFoldingRules DirectiveRules = new()
	{
		EndMarker = DirectiveEndMarkerRegex(),
		PreprocessorLine = DirectiveRegex(),
		StartMarker = DirectiveStartMarkerRegex()
	};

	/// <summary>
	/// Rules of a language whose blocks end with a word.
	/// </summary>
	private static readonly SyntaxFoldingRules EndLineRules = new()
	{
		EndLine = EndWordRegex()
	};

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(rules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 8), (2, 4), (5, 7));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a lone opening bracket under a marker that is a directive
	/// starts its block itself, as the marker heads a block of its own.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Lone_Bracket_Under_A_Directive_Marker()
	{
		// Arrange
		TextDocument document = new("""
			void F()
			#region A
			{
			    x();
			}
			#endregion
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(DirectiveRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((2, 6), (3, 5));
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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5), (2, 4));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: an end line that does not line up with the first line of
	/// a block stays out of the block.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Leaves_Out_An_End_Line_That_Does_Not_Line_Up()
	{
		// Arrange
		TextDocument document = new("""
			    if a
			        x
			end
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(EndLineRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a marked block ends at its end marker, and the end line
	/// after it stays out of the block.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Leaves_The_End_Line_After_A_Marked_Block_Out()
	{
		// Arrange
		TextDocument document = new("""
			// #region A
			x
			// #endregion
			end
			""");

		SyntaxFoldingRules rules = MarkedRules with
		{
			EndLine = EndWordRegex()
		};

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(rules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(OffSideRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((2, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: under the off-side rule a directive at the end of a block
	/// stays in the block, unlike a blank line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Off_Side_Passes_Over_The_Directives()
	{
		// Arrange
		TextDocument document = new("""
			let f x =
			    a
			#if DEBUG
			    b
			#endif
			c
			""");

		SyntaxFoldingRules rules = DirectiveRules with
		{
			IsOffSide = true
		};

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(rules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: the block of a lone opening bracket starts on its head,
	/// above the directives that stand between the two.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Passes_Over_A_Directive_Between_A_Head_And_Its_Bracket()
	{
		// Arrange
		TextDocument document = new("""
			#if NET8
			void F(int a)
			#else
			void F(long a)
			#endif
			{
			    x();
			}
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(DirectiveRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((4, 8));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a start marker that is a directive and has no end marker
	/// below it ends no block around it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Passes_Over_A_Directive_Marker_Without_An_End()
	{
		// Arrange
		TextDocument document = new("""
			class C
			{
			#region A
			    int x;
			}
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(DirectiveRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: directives at the start of their lines neither end the
	/// blocks around them nor start blocks of their own.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Passes_Over_The_Directives_At_The_Start_Of_A_Line()
	{
		// Arrange
		TextDocument document = new("""
			class P
			{
			    void M()
			    {
			#if DEBUG
			        a();
			#endif
			        b();
			#if TRACE
			        c();
			#endif
			    }
			}
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(DirectiveRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 13), (3, 12));
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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 7), (3, 6));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block marked by directives at the start of their lines
	/// stands for the blocks around it at the indentation of the lines it holds, so it does not end them.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_A_Marked_Block_Of_Directives_At_The_Indentation_Of_Its_Lines()
	{
		// Arrange
		TextDocument document = new("""
			namespace N
			{
			    int a;
			#region R
			    int b;
			    int c;
			#endregion
			}
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(DirectiveRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 8), (4, 7));
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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(MarkedRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block marked by directives that holds no lines stands for
	/// the blocks around it at the indentation of its start marker.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_An_Empty_Marked_Block_Of_Directives_At_The_Indentation_Of_Its_Marker()
	{
		// Arrange
		TextDocument document = new("""
			class C
			{
			    #region R
			    #endregion
			}
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(DirectiveRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5), (3, 4));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block whose first line opens a bracket that closes on it
	/// takes in the end line after it all the same.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_An_End_Line_Into_A_Block_That_Opens_A_Bracket()
	{
		// Arrange
		TextDocument document = new("""
			sub f(
			    a)
			    x
			end
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(EndLineRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4));
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

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(PlainRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="IndentFoldingStrategy.CreateNewFoldings" />: a block takes in the end line that lines up with its first
	/// line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_The_End_Line_Into_A_Block()
	{
		// Arrange
		TextDocument document = new("""
			def f
			    x
			end
			y
			""");

		using FoldingText text = new(document);

		IndentFoldingStrategy sut = new(EndLineRules, TabSize);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the line that closes a block marked in the way of C#.
	/// </summary>
	[GeneratedRegex(@"^\s*#endregion\b")]
	private static partial Regex DirectiveEndMarkerRegex();

	/// <summary>
	/// Matches a line of a directive of C#.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#")]
	private static partial Regex DirectiveRegex();

	/// <summary>
	/// Matches the line that opens a block marked in the way of C#.
	/// </summary>
	[GeneratedRegex(@"^\s*#region\b")]
	private static partial Regex DirectiveStartMarkerRegex();

	/// <summary>
	/// Matches the line that closes a block marked in the way of JavaScript.
	/// </summary>
	[GeneratedRegex(@"^\s*//\s*#endregion\b")]
	private static partial Regex EndMarkerRegex();

	/// <summary>
	/// Matches the text of a line that ends a block with a word alone.
	/// </summary>
	[GeneratedRegex("^end$")]
	private static partial Regex EndWordRegex();

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
