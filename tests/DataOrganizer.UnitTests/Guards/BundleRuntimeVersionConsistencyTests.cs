using AwesomeAssertions;
using System;
using System.Globalization;
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
	/// Folder of the bundle project, which also names its files.
	/// </summary>
	private const string BundleName = "Bundle";

	/// <summary>
	/// Start of the constant that carries the name of the runtime package into the bundle source.
	/// </summary>
	private const string DescriptionPrefix = "RuntimeDescription=";

	/// <summary>
	/// Start of the name of a target framework, followed by its version.
	/// </summary>
	private const string FrameworkPrefix = "net";

	/// <summary>
	/// Namespace of the WiX extension that searches for the installed runtime.
	/// </summary>
	private static readonly XNamespace Netfx = "http://wixtoolset.org/schemas/v4/wxs/netfx";
	#endregion

	#region Methods
	/// <summary>
	/// The name the bundle shows for the runtime package has the version of the target framework.
	/// </summary>
	[Test]
	public void Bundle_Describes_The_Runtime_Of_The_Target_Framework()
	{
		// Arrange
		Version framework = ReadTargetFramework();

		// Act
		string description = ReadRuntimeDescription();

		// Assert
		description
			.Should()
			.StartWith($".NET {framework} Runtime");
	}

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

	/// <summary>
	/// The bundle looks for an installed runtime of the major version of the target framework.
	/// </summary>
	[Test]
	public void Bundle_Searches_For_The_Runtime_Of_The_Target_Framework()
	{
		// Arrange
		Version framework = ReadTargetFramework();

		// Act
		string majorVersion = ReadSearchedMajorVersion();

		// Assert
		majorVersion
			.Should()
			.Be(framework.Major.ToString(CultureInfo.InvariantCulture));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Loads a file of the bundle project given by its extension.
	/// </summary>
	private static XDocument LoadBundleFile(string extension)
	{
		return XDocument.Parse(RepositoryFiles.ReadText(Path.Combine(RepositoryFiles.SourceFolder, BundleName, BundleName + extension)));
	}

	/// <summary>
	/// Reads the name of the runtime package from the constants of the bundle project.
	/// </summary>
	private static string ReadRuntimeDescription()
	{
		string constant = LoadBundleFile(".wixproj")
			.Descendants("DefineConstants")
			.Select(static x => x.Value)
			.Single(static x => x.Contains(DescriptionPrefix, StringComparison.Ordinal));

		return constant[(constant.IndexOf(DescriptionPrefix, StringComparison.Ordinal) + DescriptionPrefix.Length)..];
	}

	/// <summary>
	/// Reads the version of the runtime installer the bundle embeds.
	/// </summary>
	private static Version ReadRuntimeVersion()
	{
		string version = LoadBundleFile(".wixproj")
			.Descendants("DotNetVersion")
			.Single()
			.Value;

		return Version.Parse(version);
	}

	/// <summary>
	/// Reads the major version of the runtime the bundle searches the machine for.
	/// </summary>
	private static string ReadSearchedMajorVersion()
	{
		return LoadBundleFile(".wxs")
			.Descendants(Netfx + "DotNetCoreSearch")
			.Single()
			.Attribute("MajorVersion")!
			.Value;
	}

	/// <summary>
	/// Reads the version of the framework every project targets.
	/// </summary>
	private static Version ReadTargetFramework()
	{
		string framework = XDocument
			.Parse(RepositoryFiles.ReadText(BuildPropertiesFileName))
			.Descendants("TargetFramework")
			.Single()
			.Value;

		return Version.Parse(framework[FrameworkPrefix.Length..]);
	}
	#endregion
}
