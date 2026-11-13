using AwesomeAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the third-party notices list the packages of the application with their versions")]
internal partial class ThirdPartyNoticesVersionConsistencyTests
{
	#region Data
	/// <summary>
	/// Project of the desktop host, relative to the folder of the projects: the application is what it builds from.
	/// </summary>
	private const string HostProjectPath = "DataOrganizer.Desktop/DataOrganizer.Desktop.csproj";

	/// <summary>
	/// File of the third-party notices, relative to the repository root.
	/// </summary>
	private const string NoticesFileName = "THIRD-PARTY-NOTICES.txt";
	#endregion

	#region Methods
	/// <summary>
	/// Every package an application project references is listed in the notices with its central version.
	/// </summary>
	[Test]
	public void ThirdPartyNotices_List_Every_Application_Package_With_Its_Version()
	{
		// Arrange
		Dictionary<string, string> centralVersions = RepositoryFiles.ReadPackageVersions();

		Dictionary<string, string> listedVersions = ReadListedVersions();

		// Act
		string[] packages = [.. EnumerateApplicationPackages()];

		// Assert
		packages
			.Should()
			.NotBeEmpty()
			.And
			.AllSatisfy(x => listedVersions.GetValueOrDefault(x).Should().Be(centralVersions[x], $"{NoticesFileName} must list {x} with its version"));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Enumerates the packages the application projects reference, without those kept out of the output.
	/// </summary>
	private static IEnumerable<string> EnumerateApplicationPackages()
	{
		return EnumerateApplicationProjects()
			.SelectMany(static x => x.Descendants("PackageReference"))
			.Where(static x => x.Attribute("PrivateAssets") is null && x.Element("PrivateAssets") is null)
			.Select(static x => x.Attribute("Include")!.Value)
			.Distinct();
	}

	/// <summary>
	/// Loads the project of the desktop host and every project it references, directly or through another one.
	/// </summary>
	private static IEnumerable<XDocument> EnumerateApplicationProjects()
	{
		HashSet<string> visited = [];

		Queue<string> pending = new();

		pending.Enqueue(Path.GetFullPath(Path.Combine(RepositoryFiles.LocateRoot(), RepositoryFiles.SourceFolder, HostProjectPath)));

		while (pending.TryDequeue(out string? path))
		{
			if (!visited.Add(path))
			{
				continue;
			}

			XDocument project = XDocument.Load(path);

			foreach (XElement reference in project.Descendants("ProjectReference"))
			{
				// A project file writes the path with the Windows separator on every system.
				string relativePath = reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar);

				pending.Enqueue(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, relativePath)));
			}

			yield return project;
		}
	}

	/// <summary>
	/// Matches a line of the notices that names a package and its version.
	/// </summary>
	[GeneratedRegex(@"^[ \t]+\* (?<name>\S+) (?<version>\d\S*) - ", RegexOptions.Multiline)]
	private static partial Regex PackageLineRegex();

	/// <summary>
	/// Reads the version of each package the notices list, keyed by the package name.
	/// </summary>
	private static Dictionary<string, string> ReadListedVersions()
	{
		return PackageLineRegex()
			.Matches(RepositoryFiles.ReadText(NoticesFileName))
			.ToDictionary(static x => x.Groups["name"].Value, static x => x.Groups["version"].Value);
	}
	#endregion
}
