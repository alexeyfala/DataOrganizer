using AwesomeAssertions;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DataOrganizer.GuardTests;

[Guard]
[TestFixture(Description = "Guards the localized texts against quotes that the quote builder does not put")]
internal partial class LocalizedTextQuotesTests
{
	#region Data
	/// <summary>
	/// Resources of the neutral language, relative to the repository root.
	/// </summary>
	private const string NeutralResourcesPath = "src/DataOrganizer.Shared/Properties/Strings.resx";

	/// <summary>
	/// Resources of the Russian language, relative to the repository root.
	/// </summary>
	private const string RussianResourcesPath = "src/DataOrganizer.Shared/Properties/Strings.ru.resx";
	#endregion

	#region Methods
	/// <summary>
	/// No interpolated string with a localized text puts quotes around a hole itself; a string handed straight to the
	/// log is left out, as the log is not localized.
	/// </summary>
	[Test]
	public void Localized_Texts_Take_Their_Quotes_From_The_Quote_Builder()
	{
		// Act
		string[] quoted = [.. FindHandMadeQuotes()];

		// Assert
		quoted
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No value of the neutral resources holds the quotes of the Russian language.
	/// </summary>
	[Test]
	public void Neutral_Resources_Hold_No_Russian_Quotes()
	{
		// Act
		string[] quoted = [.. FindValuesWith(NeutralResourcesPath, '«', '»')];

		// Assert
		quoted
			.Should()
			.HaveCount(0);
	}

	/// <summary>
	/// No value of the Russian resources holds straight quotes.
	/// </summary>
	[Test]
	public void Russian_Resources_Hold_No_Straight_Quotes()
	{
		// Act
		string[] quoted = [.. FindValuesWith(RussianResourcesPath, '"')];

		// Assert
		quoted
			.Should()
			.HaveCount(0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Finds the interpolated strings with a localized text that put quotes around a hole themselves, each as its file
	/// and the string.
	/// </summary>
	private static IEnumerable<string> FindHandMadeQuotes()
	{
		foreach ((string path, string text) in RepositoryFiles.EnumerateSourceCode())
		{
			string code = CodeText.WithoutCommentsAndLiterals(text);

			foreach (Match match in InterpolatedStringRegex().Matches(code))
			{
				if (match.Value.Contains("Strings.", StringComparison.Ordinal)
					&& QuoteAtHoleRegex().IsMatch(GetText(match))
					&& !IsLogArgument(code, match.Index))
				{
					yield return $"{path}: {match.Value}";
				}
			}
		}
	}

	/// <summary>
	/// Finds the names of the resources whose values hold any of <paramref name="quotes" />.
	/// </summary>
	private static IEnumerable<string> FindValuesWith(string relativePath, params char[] quotes)
	{
		return XDocument
			.Parse(RepositoryFiles.ReadText(relativePath))
			.Root!
			.Elements("data")
			.Where(x => x.Element("value")!.Value.IndexOfAny(quotes) >= 0)
			.Select(static x => x.Attribute("name")!.Value);
	}

	/// <summary>
	/// Returns the text of an interpolated string between its delimiters, with its quotes unescaped.
	/// </summary>
	private static string GetText(Match match)
	{
		if (match.Groups["verbatim"] is { Success: true } verbatim)
		{
			return verbatim.Value.Replace(@"""""", @"""", StringComparison.Ordinal);
		}

		if (match.Groups["regular"] is { Success: true } regular)
		{
			return regular.Value.Replace(@"\""", @"""", StringComparison.Ordinal);
		}

		return match.Groups["raw"].Value;
	}

	/// <summary>
	/// Matches an interpolated string, raw, verbatim or regular, with its text between the delimiters.
	/// </summary>
	[GeneratedRegex(
		"""
		\$+(?<delimiter>"{3,})(?<raw>[\s\S]*?)\k<delimiter>
		|(?:\$@|@\$)"(?<verbatim>(?:[^"]|"")*)"
		|\$"(?<regular>(?:\\.|[^"\\\r\n])*)"
		""",
		RegexOptions.IgnorePatternWhitespace)]
	private static partial Regex InterpolatedStringRegex();

	/// <summary>
	/// <c>True</c> when the string at <paramref name="index" /> is handed straight to a log call.
	/// </summary>
	private static bool IsLogArgument(string code, int index) => LogCallEndRegex().IsMatch(code.AsSpan(0, index));

	/// <summary>
	/// Matches the end of the code right before the argument of a log call.
	/// </summary>
	[GeneratedRegex(@"\.Log\w*\s*\(\s*$")]
	private static partial Regex LogCallEndRegex();

	/// <summary>
	/// Matches a quote written next to a hole.
	/// </summary>
	[GeneratedRegex(@"""\{|\}""")]
	private static partial Regex QuoteAtHoleRegex();
	#endregion
}
