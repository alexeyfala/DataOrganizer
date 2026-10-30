using AvaloniaEdit.Document;
using AwesomeAssertions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TextProcessing.Dto;
using TextProcessing.Helpers;

namespace TextProcessing.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(MarkdownFoldingStrategy)}"" type")]
internal partial class MarkdownFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Columns from one tab stop to the next.
	/// </summary>
	private const int TabSize = 4;

	/// <summary>
	/// Rules of Markdown: the off-side rule and the markers of VS Code.
	/// </summary>
	private static readonly SyntaxFoldingRules Rules = new()
	{
		EndMarker = EndMarkerRegex(),
		IsOffSide = true,
		StartMarker = StartMarkerRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a section runs down to the next heading of its level.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Section_At_The_Next_Heading_Of_Its_Level()
	{
		// Arrange
		TextDocument document = new("# A\ntext\n# B\nmore");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 4));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a region between markers folds with its end marker.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Marked_Region()
	{
		// Arrange
		TextDocument document = new("<!-- #region A -->\ntext\n<!-- #endregion -->\nmore");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: an item of a list folds the deeper items under it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_The_Items_Of_A_Nested_List()
	{
		// Arrange
		TextDocument document = new("- a\n  - b\n- c");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a heading needs a line to fold under it, and so the last
	/// line of a text, read as a heading, keeps the section above it to the line before.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_No_Heading_Without_A_Line_Under_It()
	{
		// Arrange
		TextDocument document = new("# A\n# B");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a line underlined with equal signs or dashes is a heading
	/// of the first or the second level.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_The_Section_Of_An_Underlined_Heading()
	{
		// Arrange
		TextDocument document = new("A\n===\ntext\nB\n---\nmore");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 6), (4, 6));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: the blank line before the next heading stays in view,
	/// as in VS Code.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Blank_Line_Before_The_Next_Heading_In_View()
	{
		// Arrange
		TextDocument document = new("# A\ntext\n\n# B\nmore");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (4, 5));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a blank line in a block of code does not close it, so the
	/// lines after it are no headings.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Block_Of_Code_Open_Past_A_Blank_Line()
	{
		// Arrange
		TextDocument document = new("```\n\n# B\n```\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a section holds the deeper headings with their sections.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Deeper_Headings_In_A_Section()
	{
		// Arrange
		TextDocument document = new("# A\n## B\ntext\n# C\nmore");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3), (2, 3), (4, 5));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: the section of a heading takes its line from a block by
	/// indentation, which the section holds.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Lets_A_Section_Take_The_Line_Of_A_Block()
	{
		// Arrange
		TextDocument document = new("# A\n    code\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a heading may have six signs or no text, and up to three
	/// spaces before it.
	/// </summary>
	[Test]
	[TestCase("###### A")]
	[TestCase("#")]
	[TestCase("   # A")]
	public void CreateNewFoldings_Takes_A_Heading_At_The_Limits_Of_Its_Mark(string line)
	{
		// Arrange
		TextDocument document = new($"{line}\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: an underline makes a heading of text that only starts like
	/// an item of a list, with too many digits or without the mark after them.
	/// </summary>
	[Test]
	[TestCase("1234567890. a")]
	[TestCase("1x a")]
	public void CreateNewFoldings_Takes_An_Underline_Under_Text_That_Only_Starts_Like_An_Item(string line)
	{
		// Arrange
		TextDocument document = new($"{line}\n---\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a line of a fenced block of code is no heading, while the
	/// headings after the closing fence are.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_No_Heading_From_A_Block_Of_Code()
	{
		// Arrange
		TextDocument document = new("# A\n```\n# B\n```\n# C\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4), (5, 6));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a line indented by four spaces is code, not a heading.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_No_Heading_From_A_Line_Of_Code()
	{
		// Arrange
		TextDocument document = new("    # A\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: the front matter at the start of a text holds no heading,
	/// though its closing line looks like an underline, also when that line ends the text.
	/// </summary>
	[Test]
	[TestCase("---\ntitle: A\n---\ntext")]
	[TestCase("---\ntitle: A\n---")]
	public void CreateNewFoldings_Takes_No_Heading_From_The_Front_Matter(string content)
	{
		// Arrange
		TextDocument document = new(content);

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a number sign needs a space after it and six at most,
	/// so that a #tag stays text.
	/// </summary>
	[Test]
	[TestCase("#tag")]
	[TestCase("####### A")]
	public void CreateNewFoldings_Takes_No_Heading_Without_A_Space_Or_Past_Six_Signs(string line)
	{
		// Arrange
		TextDocument document = new($"{line}\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: dashes under a blank line, a fence or a line of code are
	/// a rule, not an underline.
	/// </summary>
	[Test]
	[TestCase("x\n\n---\ny")]
	[TestCase("```\nx\n```\n---\ny")]
	[TestCase("    x\n---\ny")]
	public void CreateNewFoldings_Takes_No_Underline_Under_A_Line_That_Is_No_Text(string content)
	{
		// Arrange
		TextDocument document = new(content);

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: dashes under an item of a list, with a bullet or with one
	/// to nine digits and a mark, are a rule, not an underline.
	/// </summary>
	[Test]
	[TestCase("- a")]
	[TestCase("1. a")]
	[TestCase("1) a")]
	[TestCase("10. a")]
	[TestCase("123456789. a")]
	public void CreateNewFoldings_Takes_No_Underline_Under_An_Item_Of_A_List(string item)
	{
		// Arrange
		TextDocument document = new($"{item}\n---\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: dashes under an underline are a rule, so the heading above
	/// keeps its section.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_No_Underline_Under_An_Underline()
	{
		// Arrange
		TextDocument document = new("A\n===\n---\ntext");

		using FoldingText text = new(document.Text);

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		TextFolding[] foldings = sut.CreateNewFoldings(text);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 4));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the line that closes a marked region of Markdown.
	/// </summary>
	[GeneratedRegex(@"^\s*<!--\s*#?endregion\b.*-->")]
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
	/// Matches the line that opens a marked region of Markdown.
	/// </summary>
	[GeneratedRegex(@"^\s*<!--\s*#?region\b.*-->")]
	private static partial Regex StartMarkerRegex();
	#endregion
}
