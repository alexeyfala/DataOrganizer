using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[TestFixture(Description = "Guards that the solution files reference describes every non-code file it covers")]
internal class SolutionFilesReferenceTests
{
	#region Data
	// The reference itself, relative to the repository root.
	private const string ReferenceFilePath = "Docs/Solution_Files.md";

	// Solution file listing the covered virtual folders.
	private const string SolutionFileName = "DataOrganizerApp.slnx";

	// Folder names holding build output rather than sources.
	private static readonly string[] BuildOutputFolders = ["bin", "obj"];

	// Virtual folders of the solution file whose files the reference covers.
	private static readonly string[] CoveredSolutionFolders = ["/Deployment/", "/Docs/", "/Solution Items/"];

	// Packaging folders on disk whose files the reference describes one by one.
	private static readonly string[] DeploymentFolders = ["Bundle", "Setup"];

	// Folders described as a whole, so their files need no individual mention.
	private static readonly string[] FoldersDescribedAsGroup = ["Docs/Images"];
	#endregion

	#region Methods
	/// <summary>
	/// Every non-code file covered by the reference is named in it.
	/// </summary>
	[Test]
	public void SolutionFilesReference_Mentions_Every_Covered_File()
	{
		// Arrange
		string reference = File.ReadAllText(Path.Combine(LocateRepositoryRoot(), ReferenceFilePath));

		// Act
		IEnumerable<string> undocumented = [.. CollectCoveredFiles().Where(path => !IsMentioned(path, reference))];

		// Assert
		undocumented
			.Should()
			.BeEmpty($"{ReferenceFilePath} must describe every file it covers");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Collects the repository-relative paths of the files the reference covers.
	/// </summary>
	private static IEnumerable<string> CollectCoveredFiles()
	{
		string root = LocateRepositoryRoot();

		IEnumerable<string> deploymentFiles = DeploymentFolders
			.SelectMany(folder => EnumerateSourceFiles(root, folder));

		return ReadSolutionFilePaths(root)
			.Concat(deploymentFiles)
			.Where(path => !IsDescribedAsGroup(path))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Order(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Enumerates the files of a repository folder, skipping build output.
	/// </summary>
	private static IEnumerable<string> EnumerateSourceFiles(string root, string folder)
	{
		return Directory
			.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'))
			.Where(path => !path.Split('/').Any(segment => BuildOutputFolders.Contains(segment, StringComparer.OrdinalIgnoreCase)));
	}

	/// <summary>
	/// Tells whether a file belongs to a folder the reference describes as a whole.
	/// </summary>
	private static bool IsDescribedAsGroup(string path)
	{
		return FoldersDescribedAsGroup.Any(folder => path.StartsWith($"{folder}/", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Tells whether the reference names the file.
	/// </summary>
	private static bool IsMentioned(string path, string reference)
	{
		return reference.Contains(Path.GetFileName(path), StringComparison.Ordinal);
	}

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
	/// Reads the file paths listed in the covered virtual folders of the solution file.
	/// </summary>
	private static IEnumerable<string> ReadSolutionFilePaths(string root)
	{
		return XDocument
			.Load(Path.Combine(root, SolutionFileName))
			.Descendants("Folder")
			.Where(folder => CoveredSolutionFolders.Any(covered => folder.Attribute("Name")!.Value.StartsWith(covered, StringComparison.Ordinal)))
			.Elements("File")
			.Select(file => file.Attribute("Path")!.Value);
	}
	#endregion
}
