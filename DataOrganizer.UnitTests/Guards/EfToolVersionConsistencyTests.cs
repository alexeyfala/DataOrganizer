using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that the dotnet-ef tool has the version of the EF Core packages of the projects")]
internal class EfToolVersionConsistencyTests
{
	#region Data
	/// <summary>
	/// Start of the name of every EF Core package.
	/// </summary>
	private const string PackagePrefix = "Microsoft.EntityFrameworkCore";

	/// <summary>
	/// Manifest of the local tools, relative to the repository root.
	/// </summary>
	private const string ToolManifestPath = ".config/dotnet-tools.json";

	/// <summary>
	/// Name of the EF Core command-line tool in the manifest.
	/// </summary>
	private const string ToolName = "dotnet-ef";
	#endregion

	#region Methods
	/// <summary>
	/// The dotnet-ef tool has the version of every EF Core package a project references.
	/// </summary>
	[Test]
	public void DotnetEf_Matches_EntityFrameworkCore_Version()
	{
		// Arrange
		string toolVersion = ReadToolVersion();

		// Act
		IEnumerable<(string Reference, string Version)> packages = [.. ReadPackageVersions()];

		// Assert
		packages
			.Should()
			.NotBeEmpty()
			.And
			.AllSatisfy(x => toolVersion.Should().Be(x.Version, $"{ToolName} must have the version of {x.Reference}"));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads the version of each EF Core package the projects of the repository reference, with the package and project names.
	/// </summary>
	private static IEnumerable<(string Reference, string Version)> ReadPackageVersions()
	{
		return Directory
			.EnumerateFiles(RepositoryFiles.LocateRoot(), "*.csproj", SearchOption.AllDirectories)
			.SelectMany(static path => XDocument
				.Load(path)
				.Descendants("PackageReference")
				.Where(static x => x.Attribute("Include")!.Value.StartsWith(PackagePrefix, StringComparison.Ordinal))
				.Select(x => (Reference: $"{x.Attribute("Include")!.Value} in {Path.GetFileName(path)}", Version: x.Attribute("Version")!.Value)));
	}

	/// <summary>
	/// Reads the version of the dotnet-ef tool from the manifest of the local tools.
	/// </summary>
	private static string ReadToolVersion()
	{
		using JsonDocument manifest = JsonDocument.Parse(RepositoryFiles.ReadText(ToolManifestPath));

		return manifest.RootElement
			.GetProperty("tools")
			.GetProperty(ToolName)
			.GetProperty("version")
			.GetString()!;
	}
	#endregion
}
