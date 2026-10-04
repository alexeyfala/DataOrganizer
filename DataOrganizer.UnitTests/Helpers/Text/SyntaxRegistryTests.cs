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
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: every language gets its rules, as .NET reads the markers of each.
	/// </summary>
	[Test]
	public void FindFoldingRules_Builds_The_Rules_Of_Every_Language()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules?[] rules = [.. sut.Languages.Select(x => sut.FindFoldingRules(x.Id!))];

		// Assert
		rules
			.Should()
			.NotContainNulls();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: the rules of a language are built once and then shared.
	/// </summary>
	[Test]
	public void FindFoldingRules_Builds_The_Rules_Once()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		SyntaxFoldingRules? first = sut.FindFoldingRules("powershell");

		// Act
		SyntaxFoldingRules? second = sut.FindFoldingRules("powershell");

		// Assert
		second
			.Should()
			.NotBeNull()
			.And
			.BeSameAs(first);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a block comment whose end is a token of blanks in the settings of
	/// a language is none, as it would close anywhere.
	/// </summary>
	[Test]
	[TestCase("diff")]
	[TestCase("ini")]
	[TestCase("properties")]
	public void FindFoldingRules_Gives_No_Block_Comments_For_A_Token_Of_Blanks(string language)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.BlockCommentStart
			.Should()
			.BeNull();

		rules.BlockCommentEnd
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: an empty token of line comments in the settings of a language gives
	/// no line comments, as it would find one on every line.
	/// </summary>
	[Test]
	public void FindFoldingRules_Gives_No_Line_Comment_For_An_Empty_Token()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules("xsl");

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.LineComment
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language whose settings mark no blocks gets no markers.
	/// </summary>
	[Test]
	public void FindFoldingRules_Gives_No_Markers_To_A_Language_Without_Them()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules("json");

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.StartMarker
			.Should()
			.BeNull();

		rules.EndMarker
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language without a grammar has no rules.
	/// </summary>
	[Test]
	[TestCase(FileEditorState.PlainTextLanguage)]
	[TestCase("unknown")]
	public void FindFoldingRules_Returns_Null_Without_A_Grammar(string language)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language takes the tokens of its block comments from its settings.
	/// </summary>
	[Test]
	[TestCase("csharp", "/*", "*/")]
	[TestCase("lua", "--[[", "]]")]
	[TestCase("powershell", "<#", "#>")]
	public void FindFoldingRules_Takes_The_Block_Comments_Of_The_Language(string language, string start, string end)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.BlockCommentStart
			.Should()
			.Be(start);

		rules.BlockCommentEnd
			.Should()
			.Be(end);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language of a preprocessor or of attributes takes the pattern of
	/// their lines.
	/// </summary>
	[Test]
	[TestCase("csharp", "#if DEBUG")]
	[TestCase("rust", "#[cfg(test)]")]
	public void FindFoldingRules_Takes_The_Directives_Of_The_Language(string language, string line)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.DirectiveLine!.IsMatch(line)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language with documentation comments of XML takes their token.
	/// </summary>
	[Test]
	[TestCase("csharp", "///")]
	[TestCase("vb", "'''")]
	public void FindFoldingRules_Takes_The_Doc_Comment_Of_The_Language(string language, string expected)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.DocComment
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language whose blocks end with a word or a tag takes the pattern
	/// of such lines.
	/// </summary>
	[Test]
	[TestCase("html", "</div>")]
	[TestCase("julia", "end")]
	[TestCase("latex", "\\end{itemize}")]
	[TestCase("lua", "end")]
	[TestCase("makefile", "endif")]
	[TestCase("razor", "</div>")]
	[TestCase("ruby", "end")]
	[TestCase("shellscript", "fi")]
	[TestCase("tex", "\\end{center}")]
	[TestCase("vb", "End Sub")]
	public void FindFoldingRules_Takes_The_End_Lines_Of_The_Language(string language, string text)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.EndLine!.IsMatch(text)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language takes the pattern of its import statements.
	/// </summary>
	[Test]
	[TestCase("csharp", "using System;")]
	[TestCase("python", "import os")]
	public void FindFoldingRules_Takes_The_Imports_Of_The_Language(string language, string line)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.ImportLine!.IsMatch(line)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language takes the token of its line comments from its settings,
	/// and Batch takes its remarks.
	/// </summary>
	[Test]
	[TestCase("csharp", "// note")]
	[TestCase("python", "# note")]
	[TestCase("vb", "' note")]
	[TestCase("bat", ":: note")]
	public void FindFoldingRules_Takes_The_Line_Comment_Of_The_Language(string language, string text)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.LineComment!.IsMatch(text)
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language takes the markers of its blocks from its settings.
	/// </summary>
	[Test]
	[TestCase("    #region Data", true, false)]
	[TestCase("    #endregion", false, true)]
	[TestCase("    // #region", false, false)]
	public void FindFoldingRules_Takes_The_Markers_Of_The_Language(string line, bool isStart, bool isEnd)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules("csharp");

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.StartMarker!.IsMatch(line)
			.Should()
			.Be(isStart);

		rules.EndMarker!.IsMatch(line)
			.Should()
			.Be(isEnd);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language takes the off-side rule from its settings.
	/// </summary>
	[Test]
	[TestCase("python", true)]
	[TestCase("yaml", true)]
	[TestCase("csharp", false)]
	[TestCase("json", false)]
	public void FindFoldingRules_Takes_The_Off_Side_Rule_Of_The_Language(string language, bool expected)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.IsOffSide
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.FindFoldingRules" />: a language of a preprocessor takes the pattern of its directives of
	/// one line.
	/// </summary>
	[Test]
	[TestCase("cpp", "#ifdef _DEBUG")]
	[TestCase("vb", "#End If")]
	public void FindFoldingRules_Takes_The_Preprocessor_Lines_Of_The_Language(string language, string line)
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SyntaxFoldingRules? rules = sut.FindFoldingRules(language);

		// Assert
		rules
			.Should()
			.NotBeNull();

		rules!.PreprocessorLine!.IsMatch(line)
			.Should()
			.BeTrue();
	}

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
		IReadOnlyList<SelectorChoice> languages = sut.Languages;

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
		IReadOnlyList<SelectorChoice> languages = sut.Languages;

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
		IReadOnlyList<SelectorChoice> languages = sut.Languages;

		// Assert
		// Diff comes with two grammar packages.
		languages.Select(static x => x.Id)
			.Should()
			.Contain("diff")
			.And
			.OnlyHaveUniqueItems();
	}

	/// <summary>
	/// <see cref="SyntaxRegistry.Languages" />: a language takes its name from its grammar package, and the extensions of
	/// its files without their dots find it.
	/// </summary>
	[Test]
	public void Languages_Take_The_Name_And_The_Extensions_Of_The_Grammar()
	{
		// Arrange
		SyntaxRegistry sut = SyntaxRegistry.Instance;

		// Act
		SelectorChoice language = sut.Languages.Single(static x => x.Id == "powershell");

		// Assert
		language.Name
			.Should()
			.Be("PowerShell");

		language.SearchTerms
			.Should()
			.Contain("ps1");
	}
	#endregion
}
