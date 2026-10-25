using AwesomeAssertions;
using EditorDocuments.Dto;
using Settings.Dto;
using Shared.Services;

namespace Settings.UnitTests.Dto;

[TestFixture(Description = "Guards that the notepad tabs left by an earlier build still open")]
internal class NotepadViewSettingsFormatCompatibilityTests
{
	#region Data
	/// <summary>
	/// Tabs of the notepad as an earlier build wrote them, with the caret position of the engine.
	/// </summary>
	private const string RecordedNotepadViewSettings = """
		{
		  "SelectedTabNumber": 2,
		  "Tabs": [
		    {
		      "EditorState": {
		        "Bookmarks": [
		          3,
		          7
		        ],
		        "CaretPosition": {
		          "Location": {
		            "Line": 3,
		            "Column": 5,
		            "IsEmpty": false
		          },
		          "Line": 3,
		          "Column": 5,
		          "VisualColumn": 4,
		          "IsAtEndOfLine": true
		        },
		        "Encoding": "utf-16",
		        "FoldedBlocks": [
		          12
		        ],
		        "FontSize": 16.5,
		        "ScrollOffset": {
		          "IsEmpty": false,
		          "X": 15,
		          "Y": 480
		        },
		        "SelectionLength": 4,
		        "SelectionStart": 20,
		        "ShowEndOfLine": true,
		        "ShowSpaces": true,
		        "ShowTabs": true,
		        "SyntaxLanguage": "python",
		        "UnfoldedBlocks": [
		          25
		        ],
		        "WordWrap": true
		      },
		      "Name": "Notes",
		      "Number": 1,
		      "Split": 0.3
		    },
		    {
		      "EditorState": null,
		      "Name": null,
		      "Number": 2,
		      "Split": null
		    }
		  ]
		}
		""";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Deserialize{T}(string)" />: the tabs of the notepad that an earlier build wrote
	/// are read whole, with the caret of the editor.
	/// </summary>
	[Test]
	public void Deserialize_Reads_The_Notepad_Tabs_Of_An_Earlier_Build()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		NotepadViewSettings? result = sut.Deserialize<NotepadViewSettings>(RecordedNotepadViewSettings);

		// Assert
		NotepadViewSettings expected = new()
		{
			SelectedTabNumber = 2,
			Tabs =
			[
				new()
				{
					EditorState = new FileEditorState
					{
						Bookmarks = [3, 7],
						CaretPosition = new()
						{
							Column = 5,
							IsAtEndOfLine = true,
							Line = 3,
							VisualColumn = 4
						},
						Encoding = "utf-16",
						FoldedBlocks = [12],
						FontSize = 16.5,
						ScrollOffset = new(15, 480),
						SelectionLength = 4,
						SelectionStart = 20,
						ShowEndOfLine = true,
						ShowSpaces = true,
						ShowTabs = true,
						SyntaxLanguage = "python",
						UnfoldedBlocks = [25],
						WordWrap = true
					},
					Name = "Notes",
					Number = 1,
					Split = 0.3
				},
				new()
				{
					EditorState = null,
					Name = null,
					Number = 2,
					Split = null
				}
			]
		};

		// A struct is compared by its members, or its arrays would be compared by reference.
		result
			.Should()
			.BeEquivalentTo(
				expected,
				options => options.ComparingByMembers<FileEditorState>().WithStrictOrdering());
	}
	#endregion
}
