using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.TestSupport.Guards;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataOrganizer.GuardTests.Guards.Localization;

[Guard]
[TestFixture(Description = "Guards the localized strings against keys that nothing uses and translations that miss or add keys")]
internal partial class LocalizedStringKeysTests
{
	#region Data
	/// <summary>
	/// Class generated from the neutral resources, relative to the source folder; it names every key without using it.
	/// </summary>
	private const string GeneratedClassPath = "DataOrganizer.Shared/Properties/Strings.Designer.cs";
	#endregion

	#region Methods
	/// <summary>
	/// The code or the markup of the application uses every key of the neutral resources; a use in the tests alone does
	/// not count.
	/// </summary>
	[Test]
	public void Application_Uses_Every_Localized_String()
	{
		// Arrange
		string[] keys = [.. LocalizedResources.ReadEntries(LocalizedResources.NeutralPath).Select(static x => x.Name)];

		HashSet<string> used = [.. FindUsedKeys()];

		// Act
		string[] unused = [.. keys.Where(x => !used.Contains(x))];

		// Assert
		keys
			.Should()
			.NotBeEmpty();

		unused
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// Every translation holds exactly the keys of the neutral resources: none left untranslated, none left over.
	/// </summary>
	[Test]
	public void Translations_Hold_The_Keys_Of_The_Neutral_Resources()
	{
		// Arrange
		string[] keys = [.. LocalizedResources.ReadEntries(LocalizedResources.NeutralPath).Select(static x => x.Name)];

		string[] translations = [.. LocalizedResources.EnumerateTranslationPaths()];

		// Act
		string[] mismatches = [.. translations.SelectMany(path => FindMismatches(path, keys))];

		// Assert
		translations
			.Should()
			.NotBeEmpty();

		mismatches
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the keys of the neutral resources a translation misses and the keys it holds beyond them.
	/// </summary>
	private static IEnumerable<string> FindMismatches(string translationPath, string[] keys)
	{
		string[] translated = [.. LocalizedResources.ReadEntries(translationPath).Select(static x => x.Name)];

		return keys
			.Except(translated)
			.Select(x => $"{translationPath}: missing {x}")
			.Concat(translated.Except(keys).Select(x => $"{translationPath}: extra {x}"));
	}

	/// <summary>
	/// Finds the keys the code and the markup of the application name, comments and string literals left out.
	/// </summary>
	private static IEnumerable<string> FindUsedKeys()
	{
		IEnumerable<string> code = RepositoryFiles
			.EnumerateSourceCode()
			.Where(static x => x.FilePath != GeneratedClassPath)
			.Select(static x => CodeText.WithoutCommentsAndLiterals(x.Text));

		IEnumerable<string> markup = RepositoryFiles
			.EnumerateProjectMarkup()
			.SelectMany(static x => x.Document
				.Descendants()
				.SelectMany(static element => element.Attributes())
				.Select(static attribute => attribute.Value)
				.Concat(x.Document.DescendantNodes().OfType<XText>().Select(static text => text.Value)));

		return code
			.Concat(markup)
			.SelectMany(static x => KeyUseRegex().Matches(x))
			.Select(static x => x.Groups["key"].Value);
	}

	/// <summary>
	/// Matches a key named on the class of the localized strings.
	/// </summary>
	[GeneratedRegex(@"\bStrings\.(?<key>\w+)")]
	private static partial Regex KeyUseRegex();
	#endregion
}
