using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the solution filter of the test run lists every test project")]
internal class SolutionFilterTests
{
	#region Data
	/// <summary>
	/// Solution filter the test run builds, relative to the repository root.
	/// </summary>
	private const string FilterFileName = "DataOrganizerApp.Tests.slnf";

	/// <summary>
	/// Folder of the test projects, relative to the repository root.
	/// </summary>
	private const string TestsFolder = "tests";
	#endregion

	#region Methods
	/// <summary>
	/// Every test project of the tests folder is listed in the solution filter, so the test run builds and runs it.
	/// </summary>
	[Test]
	public void SolutionFilter_Lists_Every_Test_Project()
	{
		// Arrange
		string[] listed = [.. ReadFilterProjects()];

		// Act
		string[] missing = [.. EnumerateTestProjects().Where(path => !listed.Contains(path, StringComparer.OrdinalIgnoreCase))];

		// Assert
		listed
			.Should()
			.NotBeEmpty();

		missing
			.Should()
			.BeEmpty($"{FilterFileName} must list every test project of {TestsFolder}/");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the repository-relative paths of the test projects, with <c>/</c> as the separator.
	/// </summary>
	private static IEnumerable<string> EnumerateTestProjects()
	{
		string root = RepositoryFiles.LocateRoot();

		return Directory
			.EnumerateFiles(Path.Combine(root, TestsFolder), "*Tests.csproj", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'));
	}

	/// <summary>
	/// Reads the project paths the solution filter lists, with <c>/</c> as the separator.
	/// </summary>
	private static IEnumerable<string> ReadFilterProjects()
	{
		using JsonDocument filter = JsonDocument.Parse(RepositoryFiles.ReadText(FilterFileName));

		return [.. filter.RootElement
			.GetProperty("solution")
			.GetProperty("projects")
			.EnumerateArray()
			.Select(static x => x.GetString()!.Replace('\\', '/'))];
	}
	#endregion
}
