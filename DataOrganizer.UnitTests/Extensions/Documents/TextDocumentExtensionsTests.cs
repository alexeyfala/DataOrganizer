using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Extensions.Documents;

namespace DataOrganizer.UnitTests.Extensions.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(TextDocumentExtensions)}"" type")]
internal class TextDocumentExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="TextDocumentExtensions.FindLineEnding" />: the style shared by the line breaks is found; a single line
	/// has none, and breaks of different styles are mixed.
	/// </summary>
	[TestCase("first\r\nsecond", LineEnding.CrLf)]
	[TestCase("first\nsecond\n", LineEnding.Lf)]
	[TestCase("first\rsecond", LineEnding.Cr)]
	[TestCase("first\nsecond\r\nthird", LineEnding.Mixed)]
	[TestCase("single line", LineEnding.None)]
	public void FindLineEnding_Finds_The_Style_Of_The_Line_Breaks(string text, LineEnding expected)
	{
		// Arrange
		TextDocument document = new(text);

		// Act
		LineEnding result = document.FindLineEnding();

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="TextDocumentExtensions.FindLineEnding" />: without a document there are no line breaks.
	/// </summary>
	[Test]
	public void FindLineEnding_Without_A_Document_Is_None()
	{
		// Arrange
		TextDocument? document = null;

		// Act
		LineEnding result = document.FindLineEnding();

		// Assert
		result
			.Should()
			.Be(LineEnding.None);
	}
	#endregion
}
