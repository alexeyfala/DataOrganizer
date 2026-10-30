using AvaloniaEdit;
using AwesomeAssertions;
using EditorDocuments.Dto;
using Shared.Services;
using System.Linq;
using System.Text.Json.Nodes;

namespace EditorDocuments.UnitTests.Dto;

[TestFixture(Description = "Guards that an earlier build still opens the editor states written now")]
internal class FileEditorStateFormatCompatibilityTests
{
	#region Methods
	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Serialize{T}" />: the caret is written in a form that the caret position of the
	/// editor engine, which an earlier build stored, reads whole.
	/// </summary>
	[Test]
	public void Serialize_Writes_A_Caret_The_Engine_Reads()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		FileEditorState state = new()
		{
			CaretPosition = new()
			{
				Column = 5,
				IsAtEndOfLine = true,
				Line = 3,
				VisualColumn = 4
			}
		};

		// Act
		string json = sut.Serialize(state);

		// Assert
		string caretJson = JsonNode
			.Parse(json)!["CaretPosition"]!
			.ToJsonString();

		TextViewPosition caret = sut.Deserialize<TextViewPosition>(caretJson);

		caret
			.Should()
			.Be(new TextViewPosition(line: 3, column: 5, visualColumn: 4)
			{
				IsAtEndOfLine = true
			});
	}

	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Serialize{T}" />: the caret is written as its line, column, visual column and
	/// end of line flag, without the location block of the editor engine that repeated the line and the column.
	/// </summary>
	[Test]
	public void Serialize_Writes_The_Caret_Without_The_Location_Block()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		FileEditorState state = new()
		{
			CaretPosition = new()
			{
				Column = 5,
				IsAtEndOfLine = true,
				Line = 3,
				VisualColumn = 4
			}
		};

		// Act
		string json = sut.Serialize(state);

		// Assert
		JsonObject caret = JsonNode
			.Parse(json)!["CaretPosition"]!
			.AsObject();

		caret.Select(x => x.Key)
			.Should()
			.BeEquivalentTo(
				"Column",
				"IsAtEndOfLine",
				"Line",
				"VisualColumn");
	}
	#endregion
}
