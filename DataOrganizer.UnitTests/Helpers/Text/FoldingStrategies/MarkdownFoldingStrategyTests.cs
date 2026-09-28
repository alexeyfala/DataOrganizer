using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2));
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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (4, 5));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: a section holds the deeper headings with their sections.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Deeper_Headings_In_A_Section()
	{
		// Arrange
		TextDocument document = new("# A\n## B\ntext\n# C\nmore");

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: any text reads as Markdown, so there is no error to report.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_No_Error()
	{
		// Arrange
		TextDocument document = new("# A\ntext");

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		sut.CreateNewFoldings(document, out int firstErrorOffset);

		// Assert
		firstErrorOffset
			.Should()
			.Be(-1);
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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: the front matter at the start of a text holds no heading,
	/// though its closing line looks like an underline.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_No_Heading_From_The_Front_Matter()
	{
		// Arrange
		TextDocument document = new("---\ntitle: A\n---\ntext");

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

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

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="MarkdownFoldingStrategy.CreateNewFoldings" />: dashes under an item of a list are a rule, not an underline.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Takes_No_Underline_Under_An_Item_Of_A_List()
	{
		// Arrange
		TextDocument document = new("- a\n---\ntext");

		MarkdownFoldingStrategy sut = new(Rules, TabSize);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.BeEmpty();
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
	private static (int Start, int End)[] GetLines(TextDocument document, IEnumerable<NewFolding> foldings)
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
