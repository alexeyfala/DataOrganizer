using AwesomeAssertions;
using DataOrganizer.Services.Documents;
using System;

namespace DataOrganizer.UnitTests.Services.Documents;

[TestFixture(Description = $@"Tests of ""{nameof(AvaloniaDocumentText)}"" type")]
internal class AvaloniaDocumentTextTests
{
	#region Methods
	/// <summary>
	/// <see cref="AvaloniaDocumentText.Changed" />: each change of the document is signalled once.
	/// </summary>
	[Test]
	public void Changed_Signals_Each_Change()
	{
		// Arrange
		int changes = 0;

		AvaloniaDocumentText sut = new();

		using IDisposable subscription = sut.Changed.Subscribe(_ => changes++);

		// Act
		sut.Document.Insert(0, "a");

		sut.Document.Insert(1, "b");

		// Assert
		changes
			.Should()
			.Be(2);
	}

	/// <summary>
	/// <see cref="AvaloniaDocumentText.Replace" />: the new text is not an edit, so undo starts from it.
	/// </summary>
	[Test]
	public void Replace_Starts_The_Undo_From_The_Text()
	{
		// Arrange
		AvaloniaDocumentText sut = new();

		sut.Document.Text = "Edit";

		// Act
		sut.Replace("Text");

		// Assert
		sut.Text
			.Should()
			.Be("Text");

		sut.Document.UndoStack.CanUndo
			.Should()
			.BeFalse();
	}
	#endregion
}
