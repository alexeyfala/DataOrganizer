using System;
using System.Buffers;
using System.Text.RegularExpressions;

namespace DataOrganizer.Helpers;

/// <summary>
/// Heuristic random-string detector: guesses whether a single plain-text token looks like a password / secret.
/// </summary>
public static partial class SensitiveTextDetector
{
	#region Data
	/// <summary>
	/// Upper bound on a credential length; longer strings are treated as free text.
	/// </summary>
	private const int MaxLength = 64;

	/// <summary>
	/// Maximum run of consecutive same-class characters; longer runs mark words / hex literals.
	/// </summary>
	private const int MaxSameClassRun = 4;

	/// <summary>
	/// Maximum allowed structural separators; 2+ mark a token as structured data, not a password.
	/// </summary>
	private const int MaxStructuralSeparators = 1;

	/// <summary>
	/// Minimum distinct character classes (lower / upper / digit / other) for a secret.
	/// </summary>
	private const int MinCharacterClasses = 3;

	/// <summary>
	/// Lower bound on a credential length.
	/// </summary>
	private const int MinLength = 8;

	/// <summary>
	/// Minimum Shannon entropy (bits per character) separating random strings from words.
	/// At length 8 the maximum possible entropy is 3.0, so raising this would drop short passwords.
	/// </summary>
	private const double MinShannonBitsPerChar = 3.0;

	/// <summary>
	/// Segmenting punctuation; 2+ occurrences mark a token as structured data (version, MAC, date,
	/// path, identifier). Password-common symbols (@ # ! $ % * + = ?) are intentionally excluded.
	/// </summary>
	private static readonly SearchValues<char> StructuralSeparators = SearchValues.Create("-._:/,;()[]{}<>");
	#endregion

	#region Methods
	/// <summary>
	/// Returns <c>True</c> when <paramref name="text" /> heuristically looks like a password / secret token.
	/// </summary>
	public static bool LooksLikeSecret(string? text)
	{
		ReadOnlySpan<char> candidate = text
			.AsSpan()
			.Trim();

		// A secret is a single token within a sane length window.
		if (candidate.Length is < MinLength or > MaxLength)
		{
			return false;
		}

		if (ContainsWhitespace(candidate))
		{
			return false;
		}

		// Negative filters: random-looking values that are NOT secrets and must stay readable.
		if (Guid.TryParse(candidate, out _)
			|| IsHexHash(candidate)
			|| EmailRegex().IsMatch(candidate)
			|| candidate.Contains('\\')
			|| PathRootRegex().IsMatch(candidate)
			|| EnvironmentVariableRegex().IsMatch(candidate)
			|| candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		// Positive signals: a password is a short, unstructured, high-entropy soup — several character
		// classes, no long single-class runs, and no separator-segmented structure.
		return CountCharacterClasses(candidate) >= MinCharacterClasses
			&& LongestSameClassRun(candidate) <= MaxSameClassRun
			&& CountStructuralSeparators(candidate) <= MaxStructuralSeparators
			&& ShannonBitsPerChar(candidate) >= MinShannonBitsPerChar;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns <c>True</c> when <paramref name="value" /> contains any whitespace character.
	/// </summary>
	private static bool ContainsWhitespace(ReadOnlySpan<char> value)
	{
		foreach (char c in value)
		{
			if (char.IsWhiteSpace(c))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Counts how many of the four classes (lower / upper / digit / other) occur in <paramref name="value" />.
	/// </summary>
	private static int CountCharacterClasses(ReadOnlySpan<char> value)
	{
		bool hasLower = false;

		bool hasUpper = false;

		bool hasDigit = false;

		bool hasOther = false;

		foreach (char c in value)
		{
			if (char.IsLower(c))
			{
				hasLower = true;
			}
			else if (char.IsUpper(c))
			{
				hasUpper = true;
			}
			else if (char.IsDigit(c))
			{
				hasDigit = true;
			}
			else
			{
				hasOther = true;
			}
		}

		return (hasLower ? 1 : 0) + (hasUpper ? 1 : 0) + (hasDigit ? 1 : 0) + (hasOther ? 1 : 0);
	}

	/// <summary>
	/// Counts structural separators in <paramref name="value" /> (see <see cref="StructuralSeparators" />).
	/// </summary>
	private static int CountStructuralSeparators(ReadOnlySpan<char> value)
	{
		int count = 0;

		foreach (char c in value)
		{
			if (StructuralSeparators.Contains(c))
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Matches a whole-string e-mail address (no surrounding whitespace).
	/// </summary>
	[GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
	private static partial Regex EmailRegex();

	/// <summary>
	/// Matches a Windows environment-variable expansion such as <c>%Temp%</c> or <c>%ProgramFiles(x86)%</c>.
	/// </summary>
	[GeneratedRegex(@"%[^%\s]+%", RegexOptions.CultureInvariant)]
	private static partial Regex EnvironmentVariableRegex();

	/// <summary>
	/// Classifies <paramref name="c" /> as 0 = lower, 1 = upper, 2 = digit, 3 = other.
	/// </summary>
	private static int GetCharClass(char c)
	{
		if (char.IsLower(c))
		{
			return 0;
		}

		if (char.IsUpper(c))
		{
			return 1;
		}

		if (char.IsDigit(c))
		{
			return 2;
		}

		return 3;
	}

	/// <summary>
	/// Returns <c>True</c> for canonical MD5 / SHA-1 / SHA-256 hex digests (32 / 40 / 64 hex chars).
	/// </summary>
	private static bool IsHexHash(ReadOnlySpan<char> value)
	{
		if (value.Length is not (32 or 40 or 64))
		{
			return false;
		}

		foreach (char c in value)
		{
			if (!Uri.IsHexDigit(c))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Length of the longest run of consecutive same-class characters. Words and hex literals have
	/// long runs; random passwords interleave classes and keep runs short.
	/// </summary>
	private static int LongestSameClassRun(ReadOnlySpan<char> value)
	{
		int longest = 0;

		int current = 0;

		int previousClass = -1;

		foreach (char c in value)
		{
			int charClass = GetCharClass(c);

			current = charClass == previousClass ? current + 1 : 1;

			previousClass = charClass;

			if (current > longest)
			{
				longest = current;
			}
		}

		return longest;
	}

	/// <summary>
	/// Matches a file-system path root: a drive (<c>C:\</c> / <c>C:/</c>), a UNC prefix (<c>\\</c>)
	/// or a home shortcut (<c>~/</c> / <c>~\</c>). Backslash paths are also caught by a separate check.
	/// </summary>
	[GeneratedRegex(@"^([A-Za-z]:[\\/]|\\\\|~[\\/])", RegexOptions.CultureInvariant)]
	private static partial Regex PathRootRegex();

	/// <summary>
	/// Shannon entropy of <paramref name="value" /> in bits per character. Length is within <see cref="MaxLength" />.
	/// </summary>
	private static double ShannonBitsPerChar(ReadOnlySpan<char> value)
	{
		Span<char> chars = stackalloc char[MaxLength];

		Span<int> counts = stackalloc int[MaxLength];

		int distinct = 0;

		foreach (char c in value)
		{
			int index = chars[..distinct].IndexOf(c);

			if (index < 0)
			{
				chars[distinct] = c;

				counts[distinct] = 1;

				distinct++;
			}
			else
			{
				counts[index]++;
			}
		}

		double entropy = 0.0;

		double length = value.Length;

		for (int i = 0; i < distinct; i++)
		{
			double probability = counts[i] / length;

			entropy -= probability * Math.Log2(probability);
		}

		return entropy;
	}
	#endregion
}
