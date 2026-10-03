using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using DataOrganizer.UnitTests.Fakes;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(CompositeFoldingStrategy)}"" type")]
internal class CompositeFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Five lines of one character each.
	/// </summary>
	private const string Text = "a\nb\nc\nd\ne";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: a block that starts inside another and ends after it is
	/// left out, as blocks have to nest.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Drops_A_Block_That_Crosses_Another()
	{
		// Arrange
		TextDocument document = new(Text);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(CreateBlock(document, 1, 3)),
			new FixedFoldingStrategy(CreateBlock(document, 2, 4)));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: a block that starts where another ends does not cross it,
	/// so both stay.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_A_Block_That_Starts_Where_Another_Ends()
	{
		// Arrange
		TextDocument document = new(Text);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(CreateBlock(document, 1, 2)),
			new FixedFoldingStrategy(CreateBlock(document, 2, 3)));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (2, 3));
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: of two blocks with the same bounds the block of the first
	/// way stays.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Block_Of_The_First_Way_When_Two_Are_Alike()
	{
		// Arrange
		TextDocument document = new(Text);

		NewFolding first = CreateBlock(document, 1, 2);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(first),
			new FixedFoldingStrategy(CreateBlock(document, 1, 2)));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.Equal(first);
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: blocks inside a block stay, one after the other.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Blocks_That_Nest()
	{
		// Arrange
		TextDocument document = new(Text);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(CreateBlock(document, 1, 5)),
			new FixedFoldingStrategy(CreateBlock(document, 2, 3), CreateBlock(document, 4, 5)));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 5), (2, 3), (4, 5));
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: of the blocks of one line the block that starts first
	/// stays, whatever way found it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_First_Block_Of_A_Line()
	{
		// Arrange
		TextDocument document = new(Text);

		// The first block holds the other one, so only their shared line tells them apart.
		NewFolding first = new(0, document.GetLineByNumber(3).EndOffset);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(CreateBlock(document, 1, 2)),
			new FixedFoldingStrategy(first));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings
			.Should()
			.Equal(first);
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: of the blocks that start together the outer one stays.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_The_Outer_Of_The_Blocks_That_Start_Together()
	{
		// Arrange
		TextDocument document = new(Text);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(CreateBlock(document, 1, 2)),
			new FixedFoldingStrategy(CreateBlock(document, 1, 3)));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 3));
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: the ways share one reading of the text.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reads_The_Text_Once_For_All_Ways()
	{
		// Arrange
		TextDocument document = new(Text);

		FixedFoldingStrategy first = new();

		FixedFoldingStrategy second = new();

		CompositeFoldingStrategy sut = new(first, second);

		// Act
		sut.CreateNewFoldings(document, out _);

		// Assert
		second.Texts
			.Should()
			.ContainSingle()
			.Which
			.Should()
			.BeSameAs(first.Texts.Single());
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: the ways read any text, so there is no error to report.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_No_Error()
	{
		// Arrange
		TextDocument document = new(Text);

		CompositeFoldingStrategy sut = new(new FixedFoldingStrategy(CreateBlock(document, 1, 2)));

		// Act
		sut.CreateNewFoldings(document, out int firstErrorOffset);

		// Assert
		firstErrorOffset
			.Should()
			.Be(-1);
	}

	/// <summary>
	/// <see cref="CompositeFoldingStrategy.CreateNewFoldings" />: the blocks of all ways come sorted by their start.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Sorts_The_Blocks_Of_All_Ways()
	{
		// Arrange
		TextDocument document = new(Text);

		CompositeFoldingStrategy sut = new(
			new FixedFoldingStrategy(CreateBlock(document, 3, 4)),
			new FixedFoldingStrategy(CreateBlock(document, 1, 2)));

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		GetLines(document, foldings)
			.Should()
			.Equal((1, 2), (3, 4));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns a block from the end of a line to the end of another, as the blocks by indentation run.
	/// </summary>
	private static NewFolding CreateBlock(TextDocument document, int startLine, int endLine)
	{
		return new NewFolding(document.GetLineByNumber(startLine).EndOffset, document.GetLineByNumber(endLine).EndOffset);
	}

	/// <summary>
	/// Returns the numbers of the first and the last lines of the blocks.
	/// </summary>
	private static (int Start, int End)[] GetLines(TextDocument document, IEnumerable<NewFolding> foldings)
	{
		return [.. foldings.Select(x => (
			document.GetLineByOffset(x.StartOffset).LineNumber,
			document.GetLineByOffset(x.EndOffset).LineNumber))];
	}
	#endregion
}
