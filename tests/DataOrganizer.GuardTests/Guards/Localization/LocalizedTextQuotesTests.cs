using AwesomeAssertions;
using DataOrganizer.GuardTests.TestHelpers;
using DataOrganizer.GuardTests.TestHelpers.Dto;
using DataOrganizer.TestSupport.Guards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.Guards.Localization;

/// <summary>
/// Guards the localized texts against quotes that the quote builder does not put.
/// </summary>
[Guard]
[TestFixture]
internal sealed partial class LocalizedTextQuotesTests
{
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
		string[] quoted = [.. FindValuesWith(LocalizedResources.NeutralPath, '«', '»')];

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
		string[] quoted = [.. FindValuesWith(LocalizedResources.RussianPath, '"')];

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
			foreach (InterpolatedString value in CodeLiteralReader.ReadInterpolatedStrings(text))
			{
				if (value.Holes.Any(static x => x.Contains("Strings.", StringComparison.Ordinal))
					&& HasQuoteAtHole(value)
					&& !IsLogArgument(text, value.Index))
				{
					yield return $"{path}: {value.Value}";
				}
			}
		}
	}

	/// <summary>
	/// Finds the names of the resources whose values hold any of <paramref name="quotes" />.
	/// </summary>
	private static IEnumerable<string> FindValuesWith(string relativePath, params char[] quotes)
	{
		return LocalizedResources
			.ReadEntries(relativePath)
			.Where(x => x.Value.IndexOfAny(quotes) >= 0)
			.Select(static x => x.Name);
	}

	/// <summary>
	/// <c>True</c> when a piece of the text of a string ends with a quote right before a hole or starts with one right
	/// after it.
	/// </summary>
	private static bool HasQuoteAtHole(InterpolatedString value)
	{
		return Enumerable
			.Range(0, value.Holes.Length)
			.Any(x => value.Texts[x].EndsWith('"') || value.Texts[x + 1].StartsWith('"'));
	}

	/// <summary>
	/// <c>True</c> when the string at <paramref name="index" /> is handed straight to a log call.
	/// </summary>
	private static bool IsLogArgument(string text, int index) => LogCallEndRegex().IsMatch(text.AsSpan(0, index));

	/// <summary>
	/// Matches the end of the code right before the argument of a log call.
	/// </summary>
	[GeneratedRegex(@"\.Log\w*\s*\(\s*$")]
	private static partial Regex LogCallEndRegex();
	#endregion
}
