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
	#region Methods
	/// <summary>
	/// Parses every markup file of the application project, each paired with its path.
	/// </summary>
	public static IEnumerable<(string FilePath, XDocument Document)> EnumerateProjectMarkup()
	{
		string root = Path.Combine(LocateRoot(), "DataOrganizer");

		return Directory
			.EnumerateFiles(root, "*.axaml", SearchOption.AllDirectories)
			.Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
			.Select(path => (FilePath: path, Document: XDocument.Load(path)));
	}

	/// <summary>
	/// Walks up from the test output directory to the folder containing Directory.Build.props.
	/// </summary>
	/// <exception cref="DirectoryNotFoundException">The tests run outside the repository.</exception>
	public static string LocateRoot()
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
	/// Reads the raw text of a file given by its repository-relative path.
	/// </summary>
	public static string ReadText(string relativePath)
	{
		return File.ReadAllText(Path.Combine(LocateRoot(), relativePath));
	}
	#endregion
}
