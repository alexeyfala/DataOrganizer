using AwesomeAssertions;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that .vscode configs stay in sync with the app name in Directory.Build.props")]
internal class VsCodeConfigConsistencyTests
{
	#region Data
	// Placeholder the .vscode configs must use instead of the literal app name.
	private const string AppNameConfigReference = "${config:app.name}";

	// MSBuild property holding the one-word app name, the canonical source of the name.
	private const string AppNamePropertyName = "AppNameAsOneWord";

	// Custom VS Code setting that mirrors the MSBuild property for ${config:app.name} references.
	private const string AppNameSettingKey = "app.name";
	#endregion

	#region Methods
	/// <summary>
	/// The "app.name" setting in .vscode/settings.json equals AppNameAsOneWord in Directory.Build.props.
	/// </summary>
	[Test]
	public void SettingsAppName_Matches_DirectoryBuildProps_AppNameAsOneWord()
	{
		// Arrange
		string expected = ReadAppNameFromDirectoryBuildProps();

		// Act
		string actual = ReadAppNameFromVsCodeSettings();

		// Assert
		actual
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// The .vscode config does not hardcode the literal app name anywhere.
	/// </summary>
	[TestCase("launch.json")]
	[TestCase("tasks.json")]
	public void VsCodeConfig_Does_Not_Hardcode_AppName(string fileName)
	{
		// Arrange
		string appName = ReadAppNameFromDirectoryBuildProps();

		// Act
		string content = ReadVsCodeFileText(fileName);

		// Assert
		content
			.Should()
			.NotContain(appName);
	}

	/// <summary>
	/// The .vscode config references the app name via ${config:app.name} rather than hardcoding it.
	/// </summary>
	[TestCase("launch.json")]
	[TestCase("tasks.json")]
	public void VsCodeConfig_Uses_AppName_Config_Reference(string fileName)
	{
		// Act
		string content = ReadVsCodeFileText(fileName);

		// Assert
		content
			.Should()
			.Contain(AppNameConfigReference);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Walks up from the test output directory to the folder containing Directory.Build.props.
	/// </summary>
	private static string LocateRepositoryRoot()
	{
		DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName
			?? throw new DirectoryNotFoundException("Could not locate the repository root (Directory.Build.props not found).");
	}

	/// <summary>
	/// Reads AppNameAsOneWord from Directory.Build.props at the repository root.
	/// </summary>
	private static string ReadAppNameFromDirectoryBuildProps()
	{
		string path = Path.Combine(LocateRepositoryRoot(), "Directory.Build.props");

		// Directory.Build.props declares <Project> without a namespace, so match by local name.
		XElement element = XDocument.Load(path)
			.Descendants()
			.Single(e => e.Name.LocalName == AppNamePropertyName);

		return element.Value.Trim();
	}

	/// <summary>
	/// Reads the "app.name" string value from .vscode/settings.json (JSON with comments).
	/// </summary>
	private static string ReadAppNameFromVsCodeSettings()
	{
		string text = ReadVsCodeFileText("settings.json");

		JsonDocumentOptions options = new()
		{
			CommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
		};

		using JsonDocument document = JsonDocument.Parse(text, options);

		return document.RootElement
			.GetProperty(AppNameSettingKey)
			.GetString()!;
	}

	/// <summary>
	/// Reads the raw text of a file inside the repository's .vscode folder.
	/// </summary>
	private static string ReadVsCodeFileText(string fileName)
	{
		string path = Path.Combine(LocateRepositoryRoot(), ".vscode", fileName);

		return File.ReadAllText(path);
	}
	#endregion
}
