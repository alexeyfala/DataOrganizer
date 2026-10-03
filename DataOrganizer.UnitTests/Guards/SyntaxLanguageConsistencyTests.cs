using AwesomeAssertions;
using DataOrganizer.Helpers.Text;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Guards;

[Guard]
[TestFixture(Description = "Guards that every language the syntax rules name still has a grammar")]
internal partial class SyntaxLanguageConsistencyTests
{
	#region Data
	/// <summary>
	/// Source files whose rules name languages by the identifiers of their grammars.
	/// </summary>
	private static readonly string[] RuleFilePaths =
	[
		Path.Combine("DataOrganizer", "Helpers", "Text", "SyntaxFolding.cs"),
		Path.Combine("DataOrganizer", "Helpers", "Text", "SyntaxLinePatterns.cs")
	];
	#endregion

	#region Methods
	/// <summary>
	/// Every language that the syntax rules name in a switch arm or in a constant has a grammar.
	/// </summary>
	/// <remarks>
	/// A language that a new version of the grammars renames only turns its rules off, so nothing but this test
	/// catches it.
	/// </remarks>
	[Test]
	public void Languages_Named_By_The_Syntax_Rules_Have_A_Grammar()
	{
		// Arrange
		string[] languages = [.. RuleFilePaths
			.Select(RepositoryFiles.ReadText)
			.SelectMany(static x => LanguageRegex().Matches(x))
			.Select(static x => x.Groups["language"].Value)
			.Distinct(StringComparer.Ordinal)
			.Order(StringComparer.Ordinal)];

		// Act
		string[] missing = [.. languages.Where(static x => SyntaxRegistry.Instance.FindScope(x) is null)];

		// Assert
		languages
			.Should()
			.NotBeEmpty();

		missing
			.Should()
			.BeEmpty();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches a language in a switch arm, such as <c>"vb" =&gt;</c>, or in a constant of a language, and captures it.
	/// </summary>
	[GeneratedRegex(@"""(?<language>[^""\s]+)""(?=\s*(?:or\b|=>))|const string \w+Language = ""(?<language>[^""]+)""")]
	private static partial Regex LanguageRegex();
	#endregion
}
