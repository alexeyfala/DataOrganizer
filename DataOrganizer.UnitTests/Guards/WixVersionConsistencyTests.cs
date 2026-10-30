using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the WiX extensions of each installer project match the version of its WiX SDK")]
internal class WixVersionConsistencyTests
{
	#region Data
	/// <summary>
	/// Start of the package name of a WiX extension.
	/// </summary>
	private const string ExtensionPrefix = "WixToolset.";

	/// <summary>
	/// End of the package name of a WiX extension.
	/// </summary>
	private const string ExtensionSuffix = ".wixext";

	/// <summary>
	/// Start of the Sdk attribute of a project header, followed by the version of the WiX SDK.
	/// </summary>
	private const string SdkPrefix = "WixToolset.Sdk/";
	#endregion

	#region Methods
	/// <summary>
	/// Every WiX extension package of the project has the version of the WiX SDK in its header.
	/// </summary>
	[TestCase("Bundle")]
	[TestCase("Setup")]
	public void WixExtensions_Match_Sdk_Version(string projectName)
	{
		// Arrange
		XDocument project = LoadProject(projectName);

		string sdkVersion = ReadSdkVersion(project);

		// Act
		Dictionary<string, string> extensionVersions = ReadExtensionVersions(project);

		// Assert
		extensionVersions
			.Should()
			.NotBeEmpty()
			.And
			.AllSatisfy(x => x.Value.Should().Be(sdkVersion, $"{x.Key} ships with the SDK of the same version"));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Loads a WiX project given by the name it shares with its folder.
	/// </summary>
	private static XDocument LoadProject(string projectName)
	{
		return XDocument.Load(Path.Combine(RepositoryFiles.LocateRoot(), projectName, $"{projectName}.wixproj"));
	}

	/// <summary>
	/// Reads the version of each WiX extension package of the project, keyed by the package name.
	/// </summary>
	private static Dictionary<string, string> ReadExtensionVersions(XDocument project)
	{
		return project
			.Descendants("PackageReference")
			.Where(static x => x.Attribute("Include")!.Value.StartsWith(ExtensionPrefix, StringComparison.Ordinal)
				&& x.Attribute("Include")!.Value.EndsWith(ExtensionSuffix, StringComparison.Ordinal))
			.ToDictionary(static x => x.Attribute("Include")!.Value, static x => x.Attribute("Version")!.Value);
	}

	/// <summary>
	/// Reads the version of the WiX SDK from the Sdk attribute of the project header.
	/// </summary>
	/// <exception cref="InvalidDataException">The header names no WiX SDK with a version.</exception>
	private static string ReadSdkVersion(XDocument project)
	{
		string sdk = project.Root!.Attribute("Sdk")!.Value;

		return sdk.StartsWith(SdkPrefix, StringComparison.Ordinal)
			? sdk[SdkPrefix.Length..]
			: throw new InvalidDataException($"The Sdk attribute '{sdk}' does not start with '{SdkPrefix}'.");
	}
	#endregion
}
