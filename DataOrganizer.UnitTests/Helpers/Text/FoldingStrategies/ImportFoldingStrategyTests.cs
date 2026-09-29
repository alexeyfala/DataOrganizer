using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(ImportFoldingStrategy)}"" type")]
internal partial class ImportFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Rules of a language that imports with the word import, with the comments of C, a preprocessor and the block markers
	/// of JavaScript.
	/// </summary>
	private static readonly SyntaxFoldingRules Rules = new()
	{
		BlockCommentEnd = "*/",
		BlockCommentStart = "/*",
		DirectiveLine = DirectiveRegex(),
		EndMarker = EndMarkerRegex(),
		ImportLine = ImportRegex(),
		LineComment = LineCommentRegex(),
		StartMarker = StartMarkerRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a marker ends a run, as it heads a block that the run must
	/// not cross.
	/// </summary>
	[Test]
	[TestCase("// #region A")]
	[TestCase("// #endregion")]
	public void CreateNewFoldings_Ends_A_Run_At_A_Marker(string marker)
	{
		// Arrange
		TextDocument document = new($"import a\nimport b\n{marker}\nimport c\nimport d");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (4, 5));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a line of other code ends a run, and the imports after it
	/// start another one.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Run_At_Other_Code()
	{
		// Arrange
		TextDocument document = new("import a\nimport b\nx\nimport c\nimport d");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (4, 5));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a block comment that stays open to the end of the text ends
	/// the search, and the run before it folds.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_At_A_Block_Comment_That_Stays_Open()
	{
		// Arrange
		TextDocument document = new("import a\nimport b\n/*\nimport c\nimport d");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: two import statements in a row fold.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Run_Of_Imports()
	{
		// Arrange
		TextDocument document = new("import a\nimport b\nx");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a run folds from the first word of its first line and the
	/// blanks after it to the end of its last line, so the keyword stays in view.
	/// </summary>
	[Test]
	[TestCase("import a\nimport b", 7)]
	[TestCase("  import  a\nimport b", 10)]
	[TestCase("import\ta\nimport b", 7)]
	[TestCase("import{a}\nimport b", 6)]
	public void CreateNewFoldings_Folds_After_The_First_Word_And_Its_Blanks(string content, int start)
	{
		// Arrange
		TextDocument document = new(content);

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((start, document.TextLength));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a language without a pattern of its imports folds nothing.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_Nothing_Without_An_Import_Pattern()
	{
		// Arrange
		TextDocument document = new("import a\nimport b");

		using FoldingText text = new(document);

		SyntaxFoldingRules rules = Rules with
		{
			ImportLine = null
		};

		ImportFoldingStrategy sut = new(rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a statement goes on over the lines while its brackets stay
	/// open.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Follows_A_Statement_Over_Its_Brackets()
	{
		// Arrange
		TextDocument document = new("import {\n  a,\n  b\n} from 'x'\nimport c\ny");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: the box of a run shows the dots, and the run starts unfolded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Gives_A_Run_No_Name()
	{
		// Arrange
		TextDocument document = new("import a\nimport b");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		// The engine declares the name as never null, while its default is null.
		foldings.Select(static x => ((string?)x.Name, x.DefaultClosed))
			.Should()
			.Equal((null, false));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: the blank lines and the comments after the last statement stay
	/// out of the run.
	/// </summary>
	[Test]
	[TestCase("")]
	[TestCase("// note")]
	[TestCase("/* note */")]
	[TestCase("#endif")]
	public void CreateNewFoldings_Keeps_The_Gaps_After_A_Run_Out(string gap)
	{
		// Arrange
		TextDocument document = new($"import a\nimport b\n{gap}\nx");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: blank lines, comments and directives may stand between the
	/// statements of a run.
	/// </summary>
	[Test]
	[TestCase("")]
	[TestCase("  ")]
	[TestCase("// note")]
	[TestCase("  // note")]
	[TestCase("/* note */")]
	[TestCase("/*\nnote\n*/")]
	[TestCase("#if DEBUG")]
	public void CreateNewFoldings_Keeps_The_Gaps_Inside_A_Run(string gap)
	{
		// Arrange
		TextDocument document = new($"import a\n{gap}\nimport b");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, document.LineCount));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: brackets that stay open to the end of the text end the search,
	/// and the runs before them fold.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Runs_Before_Brackets_That_Stay_Open()
	{
		// Arrange
		TextDocument document = new("import a\nimport b\nx\nimport c (\nimport d\nimport e");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: the brackets in a quote or in a line comment do not carry
	/// a statement over to the next line.
	/// </summary>
	[Test]
	[TestCase("import a // (")]
	[TestCase("import '('")]
	[TestCase("import \"(\"")]
	[TestCase("import `(`")]
	[TestCase("import \"\\\"(\"")]
	public void CreateNewFoldings_Leaves_Out_The_Brackets_Of_Quotes_And_Comments(string first)
	{
		// Arrange
		TextDocument document = new($"{first}\nimport b\nx");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: the end of a block comment is looked for after its start, as
	/// the two may be one token.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Looks_For_The_End_Of_A_Comment_After_Its_Start()
	{
		// Arrange
		TextDocument document = new("\"\"\"\nimport a\nimport b\n\"\"\"\nx");

		using FoldingText text = new(document);

		SyntaxFoldingRules rules = Rules with
		{
			BlockCommentEnd = "\"\"\"",
			BlockCommentStart = "\"\"\""
		};

		ImportFoldingStrategy sut = new(rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: one statement makes no run, even over several lines, which
	/// fold by their brackets instead.
	/// </summary>
	[Test]
	[TestCase("import a\nx")]
	[TestCase("import (\n  a\n)\nx")]
	public void CreateNewFoldings_Needs_Two_Statements(string content)
	{
		// Arrange
		TextDocument document = new(content);

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: the imports inside a block comment do not count.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Skips_The_Imports_In_A_Block_Comment()
	{
		// Arrange
		TextDocument document = new("/*\nimport a\nimport b\n*/\nx");

		using FoldingText text = new(document);

		ImportFoldingStrategy sut = new(Rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="ImportFoldingStrategy.CreateNewFoldings" />: a line that starts with a number sign stands in a run only in
	/// a language where it holds a directive.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_A_Number_Sign_For_A_Gap_Only_With_Directives([Values] bool hasDirectives)
	{
		// Arrange
		TextDocument document = new("import a\n#if X\nimport b");

		using FoldingText text = new(document);

		SyntaxFoldingRules rules = Rules with
		{
			DirectiveLine = hasDirectives ? DirectiveRegex() : null
		};

		ImportFoldingStrategy sut = new(rules);

		// Act
		NewFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.HaveCount(hasDirectives ? 1 : 0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches a line that starts with a number sign.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*#")]
	private static partial Regex DirectiveRegex();

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
	/// Matches a line that starts with the word import.
	/// </summary>
	[GeneratedRegex(@"^[ \t]*import\b")]
	private static partial Regex ImportRegex();

	/// <summary>
	/// Matches the token of a line comment of C at the start of a text.
	/// </summary>
	[GeneratedRegex(@"^//")]
	private static partial Regex LineCommentRegex();

	/// <summary>
	/// Matches the line that opens a block marked in the way of JavaScript.
	/// </summary>
	[GeneratedRegex(@"^\s*//\s*#region\b")]
	private static partial Regex StartMarkerRegex();
	#endregion
}
