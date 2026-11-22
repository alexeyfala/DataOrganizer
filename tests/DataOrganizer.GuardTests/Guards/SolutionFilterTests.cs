using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DataOrganizer.GuardTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the solution filter of the test run lists every test project")]
internal class SolutionFilterTests
{
	#region Data
	/// <summary>
	/// Solution filter the test run builds, relative to the repository root.
	/// </summary>
	private const string FilterFileName = "DataOrganizerApp.Tests.slnf";
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
			.HaveCount(0, $"{FilterFileName} must list every test project of {RepositoryFiles.TestsFolder}/");
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
			.EnumerateFiles(Path.Combine(root, RepositoryFiles.TestsFolder), "*Tests.csproj", SearchOption.AllDirectories)
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
