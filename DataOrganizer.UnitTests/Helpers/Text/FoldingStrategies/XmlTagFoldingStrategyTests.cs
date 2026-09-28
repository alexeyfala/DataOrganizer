using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(XmlTagFoldingStrategy)}"" type")]
internal class XmlTagFoldingStrategyTests
{
	#region Methods
	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: an element of several lines folds from its start tag to its
	/// end tag, and shows its name when folded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_An_Element_Of_Several_Lines_By_Its_Tags()
	{
		// Arrange
		TextDocument document = new("<a>\n  <b />\n</a>");

		XmlTagFoldingStrategy sut = new();

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset, x.Name))
			.Should()
			.Equal((0, document.TextLength, "<a>"));
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a text read to its end reports no error.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_No_Error_For_A_Whole_Text()
	{
		// Arrange
		TextDocument document = new("<a>\n  <b />\n</a>");

		XmlTagFoldingStrategy sut = new();

		// Act
		sut.CreateNewFoldings(document, out int firstErrorOffset);

		// Assert
		firstErrorOffset
			.Should()
			.Be(-1);
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a broken text reports where the reading stopped, so that the
	/// blocks found before stay after it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_Where_A_Broken_Text_Stops()
	{
		// Arrange
		TextDocument document = new("<a>\n  <b>\n</a>");

		XmlTagFoldingStrategy sut = new();

		// Act
		sut.CreateNewFoldings(document, out int firstErrorOffset);

		// Assert
		// The end tag of the outer element meets the open inner one.
		document.GetLineByOffset(firstErrorOffset).LineNumber
			.Should()
			.Be(3);
	}
	#endregion
}
