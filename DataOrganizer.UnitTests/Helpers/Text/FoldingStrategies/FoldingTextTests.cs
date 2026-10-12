using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using System.Linq;

namespace DataOrganizer.UnitTests.Helpers.Text.FoldingStrategies;

[TestFixture(Description = $@"Tests of ""{nameof(FoldingText)}"" type")]
internal class FoldingTextTests
{
	#region Methods
	/// <summary>
	/// <see cref="FoldingText(string)" />: the text is split into the lines the editor shows, with the same bounds.
	/// </summary>
	[TestCaseSource(nameof(Texts))]
	public void Constructor_Splits_The_Lines_As_The_Editor_Does(string text)
	{
		// Arrange
		TextDocument document = new(text);

		// Act
		using FoldingText sut = new(text);

		// Assert
		(int Start, int End)[] bounds = [.. Enumerable
			.Range(1, sut.LineCount)
			.Select(x => (sut.GetLineStart(x), sut.GetLineEnd(x)))];

		bounds
			.Should()
			.Equal(document.Lines.Select(static x => (x.Offset, x.EndOffset)));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Texts with every kind of line break, and with characters the editor does not break lines at.
	/// </summary>
	private static string[] Texts() =>
	[
		"",
		"a",
		"a\n",
		"\n",
		"a\r\nb",
		"a\rb",
		"a\n\rb",
		"a\r\r\nb\n\nc",
		"\r\n\r\n",
		"a" + (char)0x2028 + "b" + (char)0x0085 + "c\fd"
	];
	#endregion
}
