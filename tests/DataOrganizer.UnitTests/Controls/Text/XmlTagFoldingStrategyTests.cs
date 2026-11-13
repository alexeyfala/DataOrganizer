using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using AwesomeAssertions;
using DataOrganizer.Controls.Text;
using DataOrganizer.Documents.TextProcessing.Dto;
using DataOrganizer.Shared.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Controls.Text;

[TestFixture(Description = $@"Tests of ""{nameof(XmlTagFoldingStrategy)}"" type")]
internal partial class XmlTagFoldingStrategyTests
{
	#region Data
	/// <summary>
	/// Rules of XML with the region markers of its settings.
	/// </summary>
	private static readonly SyntaxFoldingRules Rules = new()
	{
		EndMarker = EndMarkerRegex(),
		StartMarker = StartMarkerRegex()
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a comment folds up to the end of its end token, also when its
	/// lines end with "\r\n".
	/// </summary>
	[Test]
	public void CreateNewFoldings_Ends_A_Comment_After_Its_End_Token()
	{
		// Arrange
		TextDocument document = new("<!--\r\n  a\r\n  b\r\n-->\r\n<c />");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((0, document.GetLineByNumber(4).EndOffset));
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a region folds from the end of the line of its start marker to
	/// the end of the line of its end marker, inside the element that holds it.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_A_Region_Between_Its_Markers()
	{
		// Arrange
		TextDocument document = new("<a>\n  <!-- #region R -->\n  <b />\n  <!-- #endregion -->\n</a>");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		// A region shows the usual mark, which the engine draws for a block without a name.
		foldings.Select(static x => (x.StartOffset, x.EndOffset, (string?)x.Name))
			.Should()
			.Equal(
				(0, document.TextLength, "<a>"),
				(document.GetLineByNumber(2).EndOffset, document.GetLineByNumber(4).EndOffset, null));
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: an element of several lines folds from its start tag to its
	/// end tag, and shows its name when folded.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_An_Element_Of_Several_Lines_By_Its_Tags()
	{
		// Arrange
		TextDocument document = new("<a>\n  <b />\n</a>");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset, x.Name))
			.Should()
			.Equal((0, document.TextLength, "<a>"));
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a document type is skipped, so the elements after it fold.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Folds_The_Elements_Of_A_Text_With_A_Document_Type()
	{
		// Arrange
		TextDocument document = new("<!DOCTYPE a SYSTEM \"a.dtd\">\n<a>\n  <b />\n</a>");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset, x.Name))
			.Should()
			.Equal((document.GetLineByNumber(2).Offset, document.TextLength, "<a>"));
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: of the elements that start on one line the first one folds,
	/// as only one block may start on a line.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Keeps_One_Block_Per_Line()
	{
		// Arrange
		TextDocument document = new("<a><b>\n</b>\n</a>");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => x.Name)
			.Should()
			.Equal("<a>");
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a region after the place where a broken text stops is left
	/// out, so the blocks the manager keeps there stay, while a region before it folds.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Leaves_Out_The_Regions_After_An_Error()
	{
		// Arrange
		// The end tag of the root element meets the open element on line 5.
		TextDocument document = new("<r>\n<!-- #region A -->\n<x />\n<!-- #endregion -->\n<b>\n</r>\n<!-- #region B -->\n<y />\n<!-- #endregion -->");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => (x.StartOffset, x.EndOffset))
			.Should()
			.Equal((document.GetLineByNumber(2).EndOffset, document.GetLineByNumber(4).EndOffset));
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a folded comment shows its first line with dots after it, as
	/// the comments of other languages do.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Names_A_Comment_After_Its_First_Line()
	{
		// Arrange
		TextDocument document = new("<!-- a  \n  b -->\n<c />");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => x.Name)
			.Should()
			.Equal($"<!-- a {Glyphs.ThreeDots}");
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a folded comment whose text starts on the next line shows its
	/// token with dots after it, not its token joined with its end.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Names_A_Comment_By_Its_Token_When_Its_Text_Starts_Below()
	{
		// Arrange
		TextDocument document = new("<!--\n  a\n-->\n<c />");

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		IEnumerable<NewFolding> foldings = sut.CreateNewFoldings(document, out _);

		// Assert
		foldings.Select(static x => x.Name)
			.Should()
			.Equal($"<!-- {Glyphs.ThreeDots}");
	}

	/// <summary>
	/// <see cref="XmlTagFoldingStrategy.CreateNewFoldings" />: a text read to its end reports no error.
	/// </summary>
	[Test]
	public void CreateNewFoldings_Reports_No_Error_For_A_Whole_Text()
	{
		// Arrange
		TextDocument document = new("<a>\n  <b />\n</a>");

		XmlTagFoldingStrategy sut = new(Rules);

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

		XmlTagFoldingStrategy sut = new(Rules);

		// Act
		sut.CreateNewFoldings(document, out int firstErrorOffset);

		// Assert
		// The end tag of the outer element meets the open inner one.
		document.GetLineByOffset(firstErrorOffset).LineNumber
			.Should()
			.Be(3);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the line that closes a region of XML.
	/// </summary>
	[GeneratedRegex(@"^\s*<!--\s*#endregion\b.*-->")]
	private static partial Regex EndMarkerRegex();

	/// <summary>
	/// Matches the line that opens a region of XML.
	/// </summary>
	[GeneratedRegex(@"^\s*<!--\s*#region\b.*-->")]
	private static partial Regex StartMarkerRegex();
	#endregion
}
