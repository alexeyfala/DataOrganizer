using Avalonia.Headless.NUnit;
using AvaloniaEdit.Document;
using AwesomeAssertions;
using DataOrganizer.Services.Documents;
using System.Linq;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(AvaloniaLogText)}"" type")]
internal class AvaloniaLogTextTests
{
	#region Methods
	/// <summary>
	/// <see cref="AvaloniaLogText.Append" />: a record that makes the log longer than the limit leaves only its last lines.
	/// </summary>
	[AvaloniaTest]
	public void Append_Keeps_The_Last_Lines()
	{
		// Arrange
		AvaloniaLogText sut = new();

		// Act
		sut.Append(CreateText(lineCount: 5), maxLines: 3);

		// Assert
		sut.Document.Text
			.Should()
			.Be("3\n4\n5");
	}

	/// <summary>
	/// <see cref="AvaloniaLogText.Document" />: the log keeps no undo history of its records.
	/// </summary>
	[AvaloniaTest]
	public void Document_Keeps_No_Undo_History()
	{
		// Arrange
		AvaloniaLogText sut = new();

		// Act
		sut.Append("record", maxLines: 3);

		// Assert
		sut.Document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="AvaloniaLogText.RemoveStartLines" />: a longer document keeps only its last lines.
	/// </summary>
	[AvaloniaTest]
	public void RemoveStartLines_Keeps_The_Last_Lines()
	{
		// Arrange
		TextDocument document = new(CreateText(lineCount: 5));

		// Act
		AvaloniaLogText.RemoveStartLines(document, maxLines: 3);

		// Assert
		document.Text
			.Should()
			.Be("3\n4\n5");
	}

	/// <summary>
	/// <see cref="AvaloniaLogText.RemoveStartLines" />: a document within the limit stays as it is.
	/// </summary>
	[AvaloniaTest]
	public void RemoveStartLines_Leaves_A_Document_Within_The_Limit([Values(2, 3)] int lineCount)
	{
		// Arrange
		string text = CreateText(lineCount);

		TextDocument document = new(text);

		// Act
		AvaloniaLogText.RemoveStartLines(document, maxLines: 3);

		// Assert
		document.Text
			.Should()
			.Be(text);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a text of lines numbered from one.
	/// </summary>
	private static string CreateText(int lineCount) => string.Join('\n', Enumerable.Range(1, lineCount));
	#endregion
}
