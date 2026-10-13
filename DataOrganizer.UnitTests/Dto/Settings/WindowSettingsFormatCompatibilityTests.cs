using AwesomeAssertions;
using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Views;
using Shared.Services;

namespace DataOrganizer.UnitTests.Dto.Settings;

[TestFixture(Description = "Guards that the window settings left by an earlier build still open")]
internal class WindowSettingsFormatCompatibilityTests
{
	#region Data
	/// <summary>
	/// Settings of the console window as an earlier build wrote them.
	/// </summary>
	private const string RecordedConsoleSettings = """
		{
		  "FontSize": 16.5,
		  "IsTopmost": true,
		  "WindowState": 2,
		  "WordWrap": true,
		  "Size": {
		    "IsEmpty": false,
		    "Width": 1024,
		    "Height": 768
		  },
		  "X": 120,
		  "Y": 80
		}
		""";

	/// <summary>
	/// Settings of the editor window as an earlier build wrote them.
	/// </summary>
	private const string RecordedEditorSettings = """
		{
		  "IsReadOnly": true,
		  "IsTopmost": true,
		  "NavigationColumnWidth": 312.5,
		  "WindowState": 2,
		  "Size": {
		    "IsEmpty": false,
		    "Width": 1280,
		    "Height": 800
		  },
		  "X": -1500,
		  "Y": 40
		}
		""";

	/// <summary>
	/// Settings of the notepad window as an earlier build wrote them.
	/// </summary>
	private const string RecordedNotepadSettings = """
		{
		  "IsTopmost": true,
		  "WindowState": 0,
		  "Size": {
		    "IsEmpty": false,
		    "Width": 700,
		    "Height": 500
		  },
		  "X": 300,
		  "Y": 200
		}
		""";
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Deserialize{T}(string)" />: the settings of the console window that an earlier
	/// build wrote are read whole.
	/// </summary>
	[Test]
	public void Deserialize_Reads_The_Console_Settings_Of_An_Earlier_Build()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		ConsoleWindowSettings? result = sut.Deserialize<ConsoleWindowSettings>(RecordedConsoleSettings);

		// Assert
		result
			.Should()
			.BeEquivalentTo(new ConsoleWindowSettings
			{
				FontSize = 16.5,
				IsTopmost = true,
				Size = new(1024, 768),
				WindowState = WindowShowState.Maximized,
				WordWrap = true,
				X = 120,
				Y = 80
			});
	}

	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Deserialize{T}(string)" />: the settings of the editor window that an earlier
	/// build wrote are read whole.
	/// </summary>
	[Test]
	public void Deserialize_Reads_The_Editor_Settings_Of_An_Earlier_Build()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		EditorWindowSettings? result = sut.Deserialize<EditorWindowSettings>(RecordedEditorSettings);

		// Assert
		result
			.Should()
			.BeEquivalentTo(new EditorWindowSettings
			{
				IsReadOnly = true,
				IsTopmost = true,
				NavigationColumnWidth = 312.5,
				Size = new(1280, 800),
				WindowState = WindowShowState.Maximized,
				X = -1500,
				Y = 40
			});
	}

	/// <summary>
	/// <see cref="SystemTextJsonSerializer.Deserialize{T}(string)" />: the settings of the notepad window that an earlier
	/// build wrote are read whole.
	/// </summary>
	[Test]
	public void Deserialize_Reads_The_Notepad_Settings_Of_An_Earlier_Build()
	{
		// Arrange
		SystemTextJsonSerializer sut = new();

		// Act
		NotepadWindowSettings? result = sut.Deserialize<NotepadWindowSettings>(RecordedNotepadSettings);

		// Assert
		result
			.Should()
			.BeEquivalentTo(new NotepadWindowSettings
			{
				IsTopmost = true,
				Size = new(700, 500),
				WindowState = WindowShowState.Normal,
				X = 300,
				Y = 200
			});
	}
	#endregion
}
