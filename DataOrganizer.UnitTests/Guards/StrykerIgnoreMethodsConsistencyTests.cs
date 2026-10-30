using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that every call the Stryker settings leave out is still made in the code")]
internal class StrykerIgnoreMethodsConsistencyTests
{
	#region Data
	/// <summary>
	/// Settings of the mutation runs, relative to the repository root.
	/// </summary>
	private const string ConfigPath = "stryker-config.json";

	/// <summary>
	/// End of a pattern that names a constructor.
	/// </summary>
	private const string ConstructorSuffix = ".ctor";
	#endregion

	#region Methods
	/// <summary>
	/// Each pattern of <c>ignore-methods</c> matches a call in the code, so a renamed call cannot leave it behind.
	/// </summary>
	[Test]
	public void IgnoreMethods_Match_A_Call_In_The_Code()
	{
		// Arrange
		string[] code = [.. ReadCode()];

		// Act
		string[] patterns = ReadIgnoredMethods();

		// Assert
		patterns
			.Should()
			.NotBeEmpty()
			.And
			.AllSatisfy(pattern => code
				.Any(ToCallRegex(pattern).IsMatch)
				.Should()
				.BeTrue($"\"{pattern}\" in {ConfigPath} must match a call in the code"));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Reads every C# file of the projects, the test projects and the build output left out.
	/// </summary>
	private static IEnumerable<string> ReadCode()
	{
		string root = RepositoryFiles.LocateRoot();

		return Directory
			.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
			.Where(path => IsProjectCode(Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar)))
			.Select(File.ReadAllText);
	}

	/// <summary>
	/// Reads the patterns of the calls the mutation runs leave out.
	/// </summary>
	private static string[] ReadIgnoredMethods()
	{
		using JsonDocument config = JsonDocument.Parse(RepositoryFiles.ReadText(ConfigPath));

		return [.. config.RootElement
			.GetProperty("stryker-config")
			.GetProperty("ignore-methods")
			.EnumerateArray()
			.Select(static x => x.GetString()!)];
	}

	/// <summary>
	/// <c>True</c> for a file of a project Stryker mutates, given by the folders of its repository-relative path.
	/// </summary>
	private static bool IsProjectCode(string[] segments)
	{
		return !segments[0].EndsWith("Tests", StringComparison.Ordinal)
			&& segments[0] != "TestSupport"
			&& !segments.Contains("bin")
			&& !segments.Contains("obj");
	}

	/// <summary>
	/// Turns a pattern into a search for its call: <c>*</c> stands for a part of a name, and a constructor is looked for after <c>new</c>.
	/// </summary>
	private static Regex ToCallRegex(string pattern)
	{
		bool isConstructor = pattern.EndsWith(ConstructorSuffix, StringComparison.Ordinal);

		string name = Regex
			.Escape(isConstructor ? pattern[..^ConstructorSuffix.Length] : pattern)
			.Replace(@"\*", @"\w*", StringComparison.Ordinal);

		return new Regex(isConstructor ? $@"\bnew\s+{name}\s*\(" : $@"{name}\s*\(");
	}
	#endregion
}
