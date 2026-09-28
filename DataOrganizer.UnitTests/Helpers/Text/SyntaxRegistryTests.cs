using AwesomeAssertions;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using TextMateSharp.Internal.Types;
using TextMateSharp.Themes;

namespace DataOrganizer.UnitTests.Helpers.Text;

[TestFixture(Description = $@"Tests of ""{nameof(SyntaxRegistry)}"" type")]
internal class SyntaxRegistryTests
{
	#region Methods
	/// <summary>
	/// <see cref="SyntaxRegistry.FindLanguage" />: the extension of the file name gives the language, whatever its case.
	/// </summary>
	[Test]
	[TestCase("script.ps1", "powershell")]
	[TestCase("SCRIPT.PS1", "powershell")]
	[TestCase("run.cmd", "bat")]
	[TestCase("setup.sh", "shellscript")]
	[TestCase("notes.md", "markdown")]
	public void FindLanguage_Follows_The_Extension(string fileName, string expected)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		string? language = sut.FindLanguage(fileName);

		// Assert
		language
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindLanguage" />: a name without a known extension gives no language.
	/// </summary>
	[Test]
	[TestCase("notes.txt")]
	[TestCase("Makefile")]
	[TestCase("")]
	[TestCase(null)]
	public void FindLanguage_Returns_Null_Without_A_Known_Extension(string? fileName)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		string? language = sut.FindLanguage(fileName);

		// Assert
		language
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindLanguage" />: a language without a grammar is passed over, as its text would stay plain.
	/// </summary>
	[Test]
	public void FindLanguage_Skips_A_Language_Without_A_Grammar()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		// Julia Markdown claims the extension, while no grammar colors its text.
		string? language = sut.FindLanguage("notes.jmd");

		// Assert
		language
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.GetColorTheme" />: the color theme suits a light or a dark background.
	/// </summary>
	[Test]
	[TestCase(false, "Light+ (default light)")]
	[TestCase(true, "Dark+ (default dark)")]
	public void GetColorTheme_Suits_The_Background(bool isDark, string expected)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		IRawTheme theme = sut.GetColorTheme(isDark);

		// Assert
		theme.GetName()
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.GetGrammar" />: a grammar is read once and then shared.
	/// </summary>
	[Test]
	public void GetGrammar_Reads_A_Grammar_Once()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		IRawGrammar? first = sut.GetGrammar("source.powershell");

		// Act
		IRawGrammar? second = sut.GetGrammar("source.powershell");

		// Assert
		second
			.Should()
			.NotBeNull()
			.And
			.BeSameAs(first);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.Languages" />: the languages are sorted by name, whatever its case.
	/// </summary>
	[Test]
	public void Languages_Are_Sorted_By_Name()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		IReadOnlyList<SyntaxLanguageChoice> languages = sut.Languages;

		// Assert
		languages
			.Should()
			.BeInAscendingOrder(static x => x.Name, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.Languages" />: every language has a grammar, as the others would leave the text plain.
	/// </summary>
	[Test]
	public void Languages_Have_A_Grammar_Each()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		IReadOnlyList<SyntaxLanguageChoice> languages = sut.Languages;

		// Assert
		languages
			.Should()
			.NotBeEmpty()
			.And
			.OnlyContain(x => sut.FindScope(x.Id!) != null);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.Languages" />: a language comes once, even with more than one grammar package.
	/// </summary>
	[Test]
	public void Languages_Hold_Each_Language_Once()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		IReadOnlyList<SyntaxLanguageChoice> languages = sut.Languages;

		// Assert
		// Diff comes with two grammar packages.
		languages.Select(static x => x.Id)
			.Should()
			.Contain("diff")
			.And
			.OnlyHaveUniqueItems();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.Languages" />: a language takes its name and the extensions of its files from its grammar package.
	/// </summary>
	[Test]
	public void Languages_Take_The_Name_And_The_Extensions_Of_The_Grammar()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxLanguageChoice language = sut.Languages.Single(static x => x.Id == "powershell");

		// Assert
		language.Name
			.Should()
			.Be("PowerShell");

		language.Extensions
			.Should()
			.Contain(".ps1");
	}
	#endregion
}
