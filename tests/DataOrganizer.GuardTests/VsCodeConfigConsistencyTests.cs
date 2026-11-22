using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace DataOrganizer.GuardTests;

[Guard]
[TestFixture(Description = "Guards that .vscode configs stay in sync with the app name and the target framework in Directory.Build.props")]
internal class VsCodeConfigConsistencyTests
{
	#region Data
	/// <summary>
	/// Placeholder the .vscode configs must use instead of the literal app name.
	/// </summary>
	private const string AppNameConfigReference = "${config:app.name}";

	/// <summary>
	/// MSBuild property holding the one-word app name, the canonical source of the name.
	/// </summary>
	private const string AppNamePropertyName = "AppName";

	/// <summary>
	/// Custom VS Code setting that mirrors the MSBuild property for ${config:app.name} references.
	/// </summary>
	private const string AppNameSettingKey = "app.name";

	/// <summary>
	/// MSBuild property holding the target framework every project of the app builds for.
	/// </summary>
	private const string AppTargetFrameworkPropertyName = "AppTargetFramework";

	/// <summary>
	/// Property of a launch configuration that holds the path of the program to start.
	/// </summary>
	private const string ProgramPropertyName = "program";
	#endregion

	#region Methods
	/// <summary>
	/// Every program path in .vscode/launch.json points at the output folder of AppTargetFramework in
	/// Directory.Build.props, with or without a platform suffix.
	/// </summary>
	[Test]
	public void LaunchConfig_Program_Paths_Use_AppTargetFramework()
	{
		// Arrange
		string expected = ReadDirectoryBuildProperty(AppTargetFrameworkPropertyName);

		// Act
		List<string> frameworks = ReadProgramFrameworks();

		// Assert
		frameworks
			.Should()
			.NotBeEmpty()
			.And
			.AllBeEquivalentTo(expected);
	}

	/// <summary>
	/// The "app.name" setting in .vscode/settings.json equals AppName in Directory.Build.props.
	/// </summary>
	[Test]
	public void SettingsAppName_Matches_DirectoryBuildProps_AppName()
	{
		// Arrange
		string expected = ReadDirectoryBuildProperty(AppNamePropertyName);

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
		string appName = ReadDirectoryBuildProperty(AppNamePropertyName);

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
	/// Returns the string values of every property named <paramref name="name" /> in an element and its descendants.
	/// </summary>
	private static IEnumerable<string> FindStrings(JsonElement element, string name)
	{
		if (element.ValueKind == JsonValueKind.Array)
		{
			foreach (JsonElement item in element.EnumerateArray())
			{
				foreach (string value in FindStrings(item, name))
				{
					yield return value;
				}
			}
		}
		else if (element.ValueKind == JsonValueKind.Object)
		{
			foreach (JsonProperty property in element.EnumerateObject())
			{
				if (property.Name == name && property.Value.ValueKind == JsonValueKind.String)
				{
					yield return property.Value.GetString()!;
				}

				foreach (string value in FindStrings(property.Value, name))
				{
					yield return value;
				}
			}
		}
	}

	/// <summary>
	/// Parses a file of the repository's .vscode folder, which may hold comments and trailing commas.
	/// </summary>
	private static JsonDocument ParseVsCodeFile(string fileName)
	{
		JsonDocumentOptions options = new()
		{
			CommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
		};

		return JsonDocument.Parse(ReadVsCodeFileText(fileName), options);
	}

	/// <summary>
	/// Reads the "app.name" string value from .vscode/settings.json.
	/// </summary>
	private static string ReadAppNameFromVsCodeSettings()
	{
		using JsonDocument document = ParseVsCodeFile("settings.json");

		return document.RootElement
			.GetProperty(AppNameSettingKey)
			.GetString()!;
	}

	/// <summary>
	/// Reads a property from Directory.Build.props at the repository root.
	/// </summary>
	private static string ReadDirectoryBuildProperty(string propertyName)
	{
		string path = Path.Combine(RepositoryFiles.LocateRoot(), "Directory.Build.props");

		// Directory.Build.props declares <Project> without a namespace, so match by local name.
		XElement element = XDocument.Load(path)
			.Descendants()
			.Single(e => e.Name.LocalName == propertyName);

		return element.Value.Trim();
	}

	/// <summary>
	/// Returns the target framework of each program path in .vscode/launch.json: the folder two levels under bin,
	/// without its platform suffix.
	/// </summary>
	private static List<string> ReadProgramFrameworks()
	{
		using JsonDocument document = ParseVsCodeFile("launch.json");

		List<string> frameworks = [];

		foreach (string program in FindStrings(document.RootElement, ProgramPropertyName))
		{
			string[] segments = program.Split('/');

			string folder = segments[Array.IndexOf(segments, "bin") + 2];

			frameworks.Add(folder.Split('-')[0]);
		}

		return frameworks;
	}

	/// <summary>
	/// Reads the raw text of a file inside the repository's .vscode folder.
	/// </summary>
	private static string ReadVsCodeFileText(string fileName)
	{
		return RepositoryFiles.ReadText(Path.Combine(".vscode", fileName));
	}
	#endregion
}
