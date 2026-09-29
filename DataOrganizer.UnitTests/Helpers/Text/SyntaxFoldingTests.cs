using Avalonia.Headless.NUnit;
using Avalonia.Input;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SyntaxFolding)}"" type")]
internal class SyntaxFoldingTests
{
	#region Data
	/// <summary>
	/// A language with a grammar.
	/// </summary>
	private const string Language = "csharp";

	/// <summary>
	/// A block from line 1 to line 4 that holds a block from line 2 to line 3.
	/// </summary>
	private const string NestedText = "a\n    b\n        c\n    d\ne";

	/// <summary>
	/// Two blocks one after the other, from line 1 to line 2 and from line 3 to line 4.
	/// </summary>
	private const string SiblingText = "a\n    b\nc\n    d";

	/// <summary>
	/// Rules of a language without markers.
	/// </summary>
	private static readonly SyntaxFoldingRules Rules = new();
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SyntaxFolding(TextArea, string, SyntaxFoldingRules)" />: the blocks of the text are there at once.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Finds_The_Blocks()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\n    b")
		};

		// Act
		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Assert
		GetFoldings(textArea).Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((textArea.Document.GetLineByNumber(1).EndOffset, textArea.Document.GetLineByNumber(2).EndOffset));
	}

	/// <summary>
	/// <see cref="SyntaxFolding(TextArea, string, SyntaxFoldingRules)" />: Markdown folds the section of a heading, which
	/// has no indentation.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Folds_Markdown_By_Its_Headings()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("# A\ntext")
		};

		SyntaxFoldingRules rules = new()
		{
			IsOffSide = true
		};

		// Act
		using SyntaxFolding sut = new(textArea, "markdown", rules);

		// Assert
		GetFoldings(textArea).Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((textArea.Document.GetLineByNumber(1).EndOffset, textArea.Document.GetLineByNumber(2).EndOffset));
	}

	/// <summary>
	/// <see cref="SyntaxFolding(TextArea, string, SyntaxFoldingRules)" />: a run of imports folds behind its keyword, beside
	/// the blocks by indentation.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Folds_The_Imports()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("using System;\nusing System.Linq;\nclass C\n{\n    int x;\n}")
		};

		SyntaxFoldingRules rules = SyntaxRegistry
			.Instance
			.FindFoldingRules(Language)!;

		// Act
		using SyntaxFolding sut = new(textArea, Language, rules);

		// Assert
		GetFoldings(textArea).Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal(
				("using ".Length, textArea.Document.GetLineByNumber(2).EndOffset),
				(textArea.Document.GetLineByNumber(3).EndOffset, textArea.Document.TextLength));
	}

	/// <summary>
	/// <see cref="SyntaxFolding(TextArea, string, SyntaxFoldingRules)" />: XML folds an element from its start tag, which
	/// is where the line of the element starts.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Folds_XML_By_Its_Tags([Values("xml", "xsl")] string language)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("<a>\n  <b />\n</a>")
		};

		// Act
		using SyntaxFolding sut = new(textArea, language, Rules);

		// Assert
		GetFoldings(textArea).Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((0, textArea.Document.TextLength));
	}

	/// <summary>
	/// <see cref="SyntaxFolding(TextArea, string, SyntaxFoldingRules)" />: the margin of the markers keeps an arrow cursor,
	/// as it would inherit the I-beam of the text area.
	/// </summary>
	[AvaloniaTest]
	public void Constructor_Gives_The_Margin_An_Arrow_Cursor()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\n    b")
		};

		// Act
		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Assert
		// A local keeps the assertion from being skipped by the null-conditional operator when there is no cursor.
		string? cursor = textArea
			.LeftMargins
			.OfType<FoldingMargin>()
			.Single()
			.Cursor?
			.ToString();

		cursor
			.Should()
			.Be(nameof(StandardCursorType.Arrow));
	}

	/// <summary>
	/// <see cref="SyntaxFolding.Dispose" />: the margin of the markers and the folded text leave the text area.
	/// </summary>
	[AvaloniaTest]
	public void Dispose_Removes_The_Folding_From_The_Text_Area()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\n    b")
		};

		SyntaxFolding sut = new(textArea, Language, Rules);

		// Act
		sut.Dispose();

		// Assert
		textArea.LeftMargins.OfType<FoldingMargin>()
			.Should()
			.BeEmpty();

		textArea.TextView.ElementGenerators.OfType<FoldingElementGenerator>()
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FindBlock" />: a line that no block holds has no block.
	/// </summary>
	[AvaloniaTest]
	public void FindBlock_Returns_Nothing_Outside_The_Blocks()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Act
		FoldingSection? block = sut.FindBlock(5);

		// Assert
		block
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FindBlock" />: a block that starts inside a folded one is out of view, so the folded one is
	/// found instead.
	/// </summary>
	[AvaloniaTest]
	public void FindBlock_Skips_A_Block_Hidden_In_A_Folded_One()
	{
		// Arrange
		// Both elements start on the first line.
		TextArea textArea = new()
		{
			Document = new("<a><b>\n</b>\n</a>")
		};

		using SyntaxFolding sut = new(textArea, "xml", Rules);

		FoldingSection outer = GetFoldings(textArea)[0];

		outer.IsFolded = true;

		// Act
		FoldingSection? block = sut.FindBlock(1);

		// Assert
		block
			.Should()
			.BeSameAs(outer);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FindBlock" />: of the blocks that hold a line, the innermost one is found, which is the one
	/// that starts on the line when there is one.
	/// </summary>
	[AvaloniaTest]
	[TestCase(1, 1)]
	[TestCase(2, 2)]
	[TestCase(3, 2)]
	[TestCase(4, 1)]
	public void FindBlock_Takes_The_Innermost_Block_Of_A_Line(int line, int startLine)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Act
		FoldingSection block = sut.FindBlock(line)!;

		// Assert
		textArea.Document.GetLineByOffset(block.StartOffset).LineNumber
			.Should()
			.Be(startLine);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FindFoldedBlock" />: the ends of a folded block stay in view, before and after the box of
	/// its hidden text.
	/// </summary>
	[AvaloniaTest]
	public void FindFoldedBlock_Keeps_The_Ends_Of_A_Folded_Block_In_View([Values] bool isEnd)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\n    b")
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection folded = GetFoldings(textArea).Single();

		folded.IsFolded = true;

		// Act
		FoldingSection? block = sut.FindFoldedBlock(isEnd ? folded.EndOffset : folded.StartOffset);

		// Assert
		block
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FindFoldedBlock" />: the text of an unfolded block is in view.
	/// </summary>
	[AvaloniaTest]
	public void FindFoldedBlock_Returns_Nothing_Inside_An_Unfolded_Block()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\n    b")
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Act
		FoldingSection? block = sut.FindFoldedBlock(textArea.Document.GetLineByNumber(2).Offset);

		// Assert
		block
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FindFoldedBlock" />: of the folded blocks around an offset, the outermost one is found, as
	/// it hides the others.
	/// </summary>
	[AvaloniaTest]
	public void FindFoldedBlock_Takes_The_Outermost_Folded_Block()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		blocks[0].IsFolded = true;

		blocks[1].IsFolded = true;

		// Act
		FoldingSection? block = sut.FindFoldedBlock(textArea.Document.GetLineByNumber(3).Offset);

		// Assert
		block
			.Should()
			.BeSameAs(blocks[0]);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.FoldAll" />: every block folds, the inner ones too.
	/// </summary>
	[AvaloniaTest]
	public void FoldAll_Folds_Every_Block()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Act
		sut.FoldAll();

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(true, true);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.GetBlockStarts" />: returns where the blocks of the asked state start.
	/// </summary>
	[AvaloniaTest]
	public void GetBlockStarts_Returns_The_Starts_Of_The_Blocks_In_A_State([Values] bool isFolded)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		blocks[1].IsFolded = true;

		// Act
		int[] starts = sut.GetBlockStarts(isFolded);

		// Assert
		starts
			.Should()
			.Equal(blocks[isFolded ? 1 : 0].StartOffset);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.HasFoldedBlocks" />: one folded block is enough.
	/// </summary>
	[AvaloniaTest]
	public void HasFoldedBlocks_Tells_Whether_A_Block_Is_Folded([Values] bool isFolded)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(SiblingText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		if (isFolded)
		{
			GetFoldings(textArea)[1].IsFolded = true;
		}

		// Act
		bool hasFoldedBlocks = sut.HasFoldedBlocks;

		// Assert
		hasFoldedBlocks
			.Should()
			.Be(isFolded);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.HasUnfoldedBlocks" />: one unfolded block is enough.
	/// </summary>
	[AvaloniaTest]
	public void HasUnfoldedBlocks_Tells_Whether_A_Block_Is_Unfolded([Values] bool isUnfolded)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(SiblingText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		blocks[0].IsFolded = true;

		blocks[1].IsFolded = !isUnfolded;

		// Act
		bool hasUnfoldedBlocks = sut.HasUnfoldedBlocks;

		// Assert
		hasUnfoldedBlocks
			.Should()
			.Be(isUnfolded);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.SetBlocksFolded" />: without an unfolded block listed, every block folds.
	/// </summary>
	[AvaloniaTest]
	public void SetBlocksFolded_Folds_Every_Block_When_None_Is_Listed_As_Unfolded()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		// Act
		sut.SetBlocksFolded([], isFolded: false);

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(true, true);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.SetBlocksFolded" />: the listed blocks fold and the others unfold.
	/// </summary>
	[AvaloniaTest]
	public void SetBlocksFolded_Folds_The_Listed_Blocks_And_Unfolds_The_Others()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		blocks[0].IsFolded = true;

		// Act
		sut.SetBlocksFolded([blocks[1].StartOffset], isFolded: true);

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(false, true);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.SetBlocksFolded" />: an offset where no block starts changes nothing, whether it is out of
	/// the text, inside a block or given twice, so no stored offset can hide a text that no block holds.
	/// </summary>
	[AvaloniaTest]
	public void SetBlocksFolded_Skips_The_Offsets_Where_No_Block_Starts([Values] bool isFolded)
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		int start = blocks[1].StartOffset;

		// Act
		sut.SetBlocksFolded(
			[int.MinValue, -1, 0, start + 1, NestedText.Length, NestedText.Length + 1, int.MaxValue, start, start],
			isFolded);

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(!isFolded, isFolded);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.SetBlocksFolded" />: the listed blocks unfold and the others fold.
	/// </summary>
	[AvaloniaTest]
	public void SetBlocksFolded_Unfolds_The_Listed_Blocks_And_Folds_The_Others()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		blocks[0].IsFolded = true;

		// Act
		sut.SetBlocksFolded([blocks[0].StartOffset], isFolded: false);

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(false, true);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.UnfoldAll" />: every block unfolds, the inner ones too.
	/// </summary>
	[AvaloniaTest]
	public void UnfoldAll_Unfolds_Every_Block()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new(NestedText)
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		FoldingSection[] blocks = GetFoldings(textArea);

		blocks[0].IsFolded = true;

		blocks[1].IsFolded = true;

		// Act
		sut.UnfoldAll();

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(false, false);
	}

	/// <summary>
	/// <see cref="SyntaxFolding.Update" />: a block that an edit makes is found.
	/// </summary>
	[AvaloniaTest]
	public void Update_Finds_The_Block_Of_An_Edit()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\nb")
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		textArea.Document.Insert(textArea.Document.TextLength, "\n    c");

		// Act
		sut.Update();

		// Assert
		GetFoldings(textArea).Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((textArea.Document.GetLineByNumber(2).EndOffset, textArea.Document.GetLineByNumber(3).EndOffset));
	}

	/// <summary>
	/// <see cref="SyntaxFolding.Update" />: a folded block stays folded after an edit elsewhere.
	/// </summary>
	[AvaloniaTest]
	public void Update_Keeps_A_Folded_Block_Folded()
	{
		// Arrange
		TextArea textArea = new()
		{
			Document = new("a\n    b\nc")
		};

		using SyntaxFolding sut = new(textArea, Language, Rules);

		GetFoldings(textArea)
			.Single()
			.IsFolded = true;

		textArea.Document.Insert(0, "d\n");

		// Act
		sut.Update();

		// Assert
		GetFoldings(textArea).Select(static x => x.IsFolded)
			.Should()
			.Equal(true);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the blocks of the text area.
	/// </summary>
	private static FoldingSection[] GetFoldings(TextArea textArea)
	{
		return [.. textArea
			.LeftMargins
			.OfType<FoldingMargin>()
			.Single()
			.FoldingManager
			.AllFoldings];
	}
	#endregion
}
