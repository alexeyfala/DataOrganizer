using AwesomeAssertions;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the bundle installs the runtime of the framework the application targets")]
internal class BundleRuntimeVersionConsistencyTests
{
	#region Data
	/// <summary>
	/// File of the build properties every project shares, relative to the repository root.
	/// </summary>
	private const string BuildPropertiesFileName = "Directory.Build.props";

	/// <summary>
	/// Folder of the bundle project, which also names its project file.
	/// </summary>
	private const string BundleName = "Bundle";

	/// <summary>
	/// Start of the name of a target framework, followed by its version.
	/// </summary>
	private const string FrameworkPrefix = "net";
	#endregion

	#region Methods
	/// <summary>
	/// The runtime installer the bundle embeds is a patch of the target framework.
	/// </summary>
	[Test]
	public void Bundle_Installs_The_Runtime_Of_The_Target_Framework()
	{
		// Arrange
		Version framework = ReadTargetFramework();

		// Act
		Version runtime = ReadRuntimeVersion();

		// Assert
		runtime.ToString(fieldCount: 2)
			.Should()
			.Be(framework.ToString());
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the version of the runtime installer the bundle embeds.
	/// </summary>
	private static Version ReadRuntimeVersion()
	{
		string version = XDocument
			.Parse(RepositoryFiles.ReadText(Path.Combine(RepositoryFiles.SourceFolder, BundleName, BundleName + ".wixproj")))
			.Descendants("DotNetVersion")
			.Single()
			.Value;

		return Version.Parse(version);
	}

	/// <summary>
	/// Reads the version of the framework every project targets.
	/// </summary>
	private static Version ReadTargetFramework()
	{
		string framework = XDocument
			.Parse(RepositoryFiles.ReadText(BuildPropertiesFileName))
			.Descendants("AppTargetFramework")
			.Single()
			.Value;

		return Version.Parse(framework[FrameworkPrefix.Length..]);
	}
	#endregion
}
