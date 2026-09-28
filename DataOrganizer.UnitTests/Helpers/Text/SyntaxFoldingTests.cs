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
