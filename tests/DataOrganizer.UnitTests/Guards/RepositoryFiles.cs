using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

/// <summary>
/// The files of the repository a guard reads, located from the test output directory.
/// </summary>
internal static class RepositoryFiles
{
	#region Data
	/// <summary>
	/// File of the central package versions, relative to the repository root.
	/// </summary>
	private const string PackageVersionsFileName = "Directory.Packages.props";

	/// <summary>
	/// Solution file at the repository root.
	/// </summary>
	public const string SolutionFileName = "DataOrganizerApp.slnx";

	/// <summary>
	/// Folder of the projects of the application, relative to the repository root.
	/// </summary>
	public const string SourceFolder = "src";
	#endregion

	#region Methods
	/// <summary>
	/// Reads every C# file of the application project, each paired with its path.
	/// </summary>
	public static IEnumerable<(string FilePath, string Text)> EnumerateProjectCode()
	{
		return EnumerateProjectFiles("*.cs")
			.Select(path => (FilePath: path, Text: File.ReadAllText(path)));
	}

	/// <summary>
	/// Parses every markup file of the application project with the line of each element, each paired with its path.
	/// </summary>
	public static IEnumerable<(string FilePath, XDocument Document)> EnumerateProjectMarkup()
	{
		return EnumerateProjectFiles("*.axaml")
			.Select(path => (FilePath: path, Document: XDocument.Load(path, LoadOptions.SetLineInfo)));
	}

	/// <summary>
	/// Walks up from the test output directory to the folder containing the solution file.
	/// </summary>
	/// <exception cref="DirectoryNotFoundException">The tests run outside the repository.</exception>
	public static string LocateRoot()
	{
		DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);

		while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
		{
			directory = directory.Parent;
		}

		return directory?.FullName
			?? throw new DirectoryNotFoundException($"Could not locate the repository root ({SolutionFileName} not found).");
	}

	/// <summary>
	/// Reads the central version of every package, keyed by the package name.
	/// </summary>
	public static Dictionary<string, string> ReadPackageVersions()
	{
		return XDocument
			.Parse(ReadText(PackageVersionsFileName))
			.Descendants("PackageVersion")
			.ToDictionary(static x => x.Attribute("Include")!.Value, static x => x.Attribute("Version")!.Value);
	}

	/// <summary>
	/// Reads the raw text of a file given by its repository-relative path.
	/// </summary>
	public static string ReadText(string relativePath)
	{
		return File.ReadAllText(Path.Combine(LocateRoot(), relativePath));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Paths of the files of the application project that match the pattern, the generated ones left out.
	/// </summary>
	private static IEnumerable<string> EnumerateProjectFiles(string searchPattern)
	{
		string root = Path.Combine(LocateRoot(), SourceFolder, "DataOrganizer");

		return Directory
			.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories)
			.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
	}
	#endregion
}
