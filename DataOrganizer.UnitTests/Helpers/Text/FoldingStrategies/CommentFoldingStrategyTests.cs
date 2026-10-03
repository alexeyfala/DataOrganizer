using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(CommentFoldingStrategy)}"" type")]
internal partial class CommentFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Rules of a language with the comments of C#, the documentation included, and the block markers of JavaScript.
	/// </summary>
	private static readonly SyntaxFoldingRules Rules = new()
	{
		BlockCommentEnd = "*/",
		BlockCommentStart = "/*",
		DocComment = "///",
		EndMarker = EndMarkerRegex(),
		LineComment = LineCommentRegex(),
		StartMarker = StartMarkerRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a block comment folds up to its end token, so the code after
	/// it stays in view.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Block_Comment_After_Its_End_Token()
	{
		// Arrange
		TextDocument document = new("  /* a\n  b */ x();");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((2, document.Text.IndexOf("*/", StringComparison.Ordinal) + 2));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a documentation comment ends at a blank line, as in C#, and
	/// the lines after it start another one.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Doc_Comment_At_A_Blank_Line()
	{
		// Arrange
		TextDocument document = new("/// a\n/// b\n\n/// c\n/// d");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (4, 5));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a block comment ends a group of line comments and folds on
	/// its own.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Group_At_A_Block_Comment()
	{
		// Arrange
		TextDocument document = new("// a\n// b\n/* c\nd */");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 4));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a line comment of another indentation starts a group of its
	/// own.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Group_At_Another_Indentation()
	{
		// Arrange
		TextDocument document = new("// a\n// b\n  // c\n  // d");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 4));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a line of code ends a group, and the comments after it start
	/// another one.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Group_At_Other_Code()
	{
		// Arrange
		TextDocument document = new("// a\n// b\nx\n// c\n// d");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (4, 5));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a block comment over several lines folds.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Block_Comment_Of_Several_Lines()
	{
		// Arrange
		TextDocument document = new("/* a\nb */\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a group folds from the token of its first line, after the
	/// indentation, to the end of its last line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Group_From_Its_Token_To_The_End_Of_Its_Last_Line()
	{
		// Arrange
		TextDocument document = new("  // a\n  // b\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((2, document.GetLineByNumber(2).EndOffset));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: line comments in a row fold as one group.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Group_Of_Line_Comments()
	{
		// Arrange
		TextDocument document = new("// a\n// b\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the lines of a documentation comment fold apart from the
	/// line comments around them.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_Doc_Comments_Apart_From_Line_Comments()
	{
		// Arrange
		TextDocument document = new("// a\n// b\n/// c\n/// d\n// e\n// f");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 4), (5, 6));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a block comment of one line has nothing to fold.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_Nothing_For_A_Block_Comment_Of_One_Line()
	{
		// Arrange
		TextDocument document = new("/* a */\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a language without the tokens of comments folds no comments.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_Nothing_Without_Comment_Tokens()
	{
		// Arrange
		TextDocument document = new("// a\n// b\n/* c\nd */");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(new SyntaxFoldingRules());

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the blank lines after the last line of a group stay out of it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_Blank_Lines_After_A_Group_Out()
	{
		// Arrange
		TextDocument document = new("// a\n// b\n\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: blank lines may stand between the lines of a group, as in
	/// Visual Studio.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_Blank_Lines_Inside_A_Group()
	{
		// Arrange
		TextDocument document = new("// a\n\n  \n// b");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a marker stays out of a group, as it heads a block of its own.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Leaves_The_Markers_Out_Of_A_Group()
	{
		// Arrange
		TextDocument document = new("// #region A\n// a\n// b\n// #endregion");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((2, 3));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the end of a block comment is looked for after its start, as
	/// the two may be one token.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Looks_For_The_End_Of_A_Block_Comment_After_Its_Start()
	{
		// Arrange
		TextDocument document = new("\"\"\"\nDoc\n\"\"\"");

		using FoldingText text = new(document);

		SyntaxFoldingRules rules = Rules with
		{
			BlockCommentEnd = "\"\"\"",
			BlockCommentStart = "\"\"\""
		};

		CommentFoldingStrategy sut = new(rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the box of a folded block comment shows its first line with
	/// dots after it, and the comment starts unfolded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Names_A_Block_Comment_After_Its_First_Line()
	{
		// Arrange
		TextDocument document = new("  /** Doc  \n   */");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.Name, x.DefaultClosed))
			.Should()
			.Equal(($"/** Doc {Glyphs.ThreeDots}", false));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the box of a folded documentation comment shows the plain
	/// text of its summary, and the comment starts unfolded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Names_A_Doc_Comment_After_Its_Summary()
	{
		// Arrange
		TextDocument document = new("  /// <summary>\n  /// Calls <see cref=\"Run\"/>.\n  /// </summary>");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.Name, x.DefaultClosed))
			.Should()
			.Equal(("/// <summary> Calls Run.", false));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the box of a folded documentation comment without a summary
	/// shows its first line with dots after it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Names_A_Doc_Comment_Without_A_Summary_After_Its_First_Line()
	{
		// Arrange
		TextDocument document = new("/// <param name=\"x\">X</param>  \n/// <returns>Y</returns>");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => x.Name)
			.Should()
			.Equal($"/// <param name=\"x\">X</param> {Glyphs.ThreeDots}");
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the box of a folded group shows its first line with dots
	/// after it, and the group starts unfolded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Names_A_Group_After_Its_First_Line()
	{
		// Arrange
		TextDocument document = new("  // a  \n  // b");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.Name, x.DefaultClosed))
			.Should()
			.Equal(($"// a {Glyphs.ThreeDots}", false));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: one line comment makes no group.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Needs_Two_Lines_For_A_Group()
	{
		// Arrange
		TextDocument document = new("// a\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: the summary of a documentation comment is read up to the
	/// end of its last line, where a summary without its end tag stops.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reads_A_Summary_Up_To_The_End_Of_The_Doc_Comment()
	{
		// Arrange
		TextDocument document = new("/// <summary>\n/// Calls Run\nx");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => x.Name)
			.Should()
			.Equal("/// <summary> Calls Run");
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a block comment that stays open holds the rest of the text,
	/// so the line comments in it make no group.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Stops_At_A_Block_Comment_That_Stays_Open()
	{
		// Arrange
		TextDocument document = new("// a\n// b\n/* c\n// d\n// e");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a line that opens a block comment is taken for one even
	/// when the token of a line comment starts it, as in Lua.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_A_Block_Comment_Before_A_Line_Comment()
	{
		// Arrange
		TextDocument document = new("--[[ a\nb ]]");

		using FoldingText text = new(document);

		SyntaxFoldingRules rules = Rules with
		{
			BlockCommentEnd = "]]",
			BlockCommentStart = "--[[",
			LineComment = LuaLineCommentRegex()
		};

		CommentFoldingStrategy sut = new(rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a longer run of slashes than the token of a documentation
	/// comment starts a line comment, as in C#.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_A_Longer_Run_Of_Slashes_For_A_Line_Comment()
	{
		// Arrange
		TextDocument document = new("/// a\n/// b\n//// c\n//// d");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 4));
	}

	/// <summary>
	/// <see cref="CommentFoldingStrategy.CreateNewFoldings" />: a comment after code on its line joins no group.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_No_Comment_After_Code_Into_A_Group()
	{
		// Arrange
		TextDocument document = new("x // a\ny // b");

		using FoldingText text = new(document);

		CommentFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
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
	/// Matches the token of a line comment of C at the start of a text.
	/// </summary>
	[GeneratedRegex(@"^//")]
	private static partial Regex LineCommentRegex();

	/// <summary>
	/// Matches the token of a line comment of Lua at the start of a text.
	/// </summary>
	[GeneratedRegex(@"^--")]
	private static partial Regex LuaLineCommentRegex();

	/// <summary>
	/// Matches the line that opens a block marked in the way of JavaScript.
	/// </summary>
	[GeneratedRegex(@"^\s*//\s*#region\b")]
	private static partial Regex StartMarkerRegex();
	#endregion
}
