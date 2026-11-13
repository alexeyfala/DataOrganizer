using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards the order of the topics inside each layer project")]
internal class TopicOrderTests
{
	#region Data
	/// <summary>
	/// Each layer project with its topic folders in order: a topic may use only the topics listed before it, and the
	/// files outside the topic folders come first.
	/// </summary>
	private static readonly object[] TopicOrders =
	[
		new object[] { "DataOrganizer.Database", new[] { "Entities", "Repository" } },
		new object[] { "DataOrganizer.Documents", new[] { "TextProcessing", "DatasetRecords", "Editor", "FileContents", "Notepad", "Notes" } },
		new object[] { "DataOrganizer.Hierarchy", new[] { "Favorites", "FilteredLists", "Storage" } },
		new object[] { "DataOrganizer.Infrastructure", new[] { "WindowContracts", "DialogContracts", "Notifications", "Diagnostics", "FileSystemOperations", "AppInstance", "Settings", "AppUpdates" } },
		new object[] { "DataOrganizer.Security", Array.Empty<string>() },
		new object[] { "DataOrganizer.Shared", new[] { "Threading" } },
		new object[] { "DataOrganizer.SystemIntegration", new[] { "Clipboard", "FileExecution", "GlobalHotkeys" } },
		new object[] { "DataOrganizer.Workflows", new[] { "AppLifecycle", "HierarchyExchange", "HierarchyScenarios", "SampleSeeding" } },
	];
	#endregion

	#region Methods
	/// <summary>
	/// Every topic folder of the project is listed, and every listed topic has its folder, so a new topic cannot stay
	/// outside the order.
	/// </summary>
	[TestCaseSource(nameof(TopicOrders))]
	public void Topic_Folders_Match_The_Order(string project, string[] topics)
	{
		// Act
		string[] folders = [.. EnumerateTopicFolders(LocateProject(project))];

		// Assert
		folders
			.Should()
			.BeEquivalentTo(topics);
	}

	/// <summary>
	/// No file of a topic has a <c>using</c> of a topic of the same project listed after its own.
	/// </summary>
	[TestCaseSource(nameof(TopicOrders))]
	public void Topics_Use_No_Topic_Listed_After_Them(string project, string[] topics)
	{
		// Arrange
		string root = LocateProject(project);

		Regex topicUsing = new($@"^using\s+(?:static\s+)?(?:\w+\s*=\s*)?{Regex.Escape(project)}\.(?<topic>\w+)", RegexOptions.Multiline);

		// Act
		string[] violations =
		[
			.. EnumerateCode(root)
				.SelectMany(file => topicUsing
					.Matches(File.ReadAllText(file))
					.Select(static x => x.Groups["topic"].Value)
					.Where(topic => Array.IndexOf(topics, topic) > Array.IndexOf(topics, ReadTopic(root, file)))
					.Select(topic => $"{Path.GetRelativePath(root, file)} uses {topic}"))
		];

		// Assert
		violations
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the C# files of a project folder, the build output left out.
	/// </summary>
	private static IEnumerable<string> EnumerateCode(string root)
	{
		return Directory
			.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
			.Where(path => !IsBuildOutput(ReadTopic(root, path)));
	}

	/// <summary>
	/// Enumerates the names of the topic folders of a project folder: the top folders that hold no code themselves.
	/// </summary>
	private static IEnumerable<string> EnumerateTopicFolders(string root)
	{
		return Directory
			.EnumerateDirectories(root)
			.Where(static path => !IsBuildOutput(Path.GetFileName(path)) && !Directory.EnumerateFiles(path, "*.cs").Any())
			.Select(static path => Path.GetFileName(path));
	}

	/// <summary>
	/// Tells whether a top folder of a project holds the build output.
	/// </summary>
	private static bool IsBuildOutput(string topFolder)
	{
		return topFolder is "bin" or "obj";
	}

	/// <summary>
	/// Locates the folder of a layer project.
	/// </summary>
	private static string LocateProject(string project)
	{
		return Path.Combine(RepositoryFiles.LocateRoot(), RepositoryFiles.SourceFolder, project);
	}

	/// <summary>
	/// Reads the top folder of a file's project-relative path, which names its topic when the file lies in a topic.
	/// </summary>
	private static string ReadTopic(string root, string file)
	{
		return Path
			.GetRelativePath(root, file)
			.Split(Path.DirectorySeparatorChar)[0];
	}
	#endregion
}
