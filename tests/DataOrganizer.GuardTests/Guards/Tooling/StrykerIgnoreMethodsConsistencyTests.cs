using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.Tooling;

[Guard]
[TestFixture(Description = "Guards that every call the Stryker settings leave out is still made in the code")]
internal sealed class StrykerIgnoreMethodsConsistencyTests
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
		string[] code =
		[
			.. RepositoryFiles
				.EnumerateSourceCode()
				.Select(static x => x.Text)
		];

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
	/// Turns a pattern into a search for the calls Stryker leaves out with it, regardless of case: one that starts with a
	/// dot is a call after <c>?</c>, any other starts a part of a name, and a constructor is looked for after <c>new</c>.
	/// </summary>
	private static Regex ToCallRegex(string pattern)
	{
		if (pattern.EndsWith(ConstructorSuffix, StringComparison.Ordinal))
		{
			return new Regex($@"\bnew\s+{ToNameRegex(pattern[..^ConstructorSuffix.Length])}\s*\(", RegexOptions.IgnoreCase);
		}

		string start = pattern.StartsWith('.') ? @"\?" : @"(?<!\w)";

		return new Regex($@"{start}{ToNameRegex(pattern)}\s*(?:<[^<>()]*>)?\s*\(", RegexOptions.IgnoreCase);
	}

	/// <summary>
	/// Turns the name of a pattern into a search: a dot may stand between spaces, and <c>*</c> stands for a part of a name.
	/// </summary>
	private static string ToNameRegex(string name)
	{
		return Regex
			.Escape(name)
			.Replace(@"\.", @"\s*\.\s*", StringComparison.Ordinal)
			.Replace(@"\*", @"\w*", StringComparison.Ordinal);
	}
	#endregion
}
