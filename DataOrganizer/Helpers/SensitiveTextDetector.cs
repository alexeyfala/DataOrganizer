using System;
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
	/// Minimum distinct character classes (lower / upper / digit / other) for a secret.
	/// </summary>
	private const int MinCharacterClasses = 3;

	/// <summary>
	/// Lower bound on a credential length.
	/// </summary>
	private const int MinLength = 8;

	/// <summary>
	/// Minimum Shannon entropy (bits per character) separating random strings from words.
	/// </summary>
	private const double MinShannonBitsPerChar = 3.0;
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
			|| candidate.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			|| candidate.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		// Positive signals: character-class variety plus high per-character entropy.
		return CountCharacterClasses(candidate) >= MinCharacterClasses && ShannonBitsPerChar(candidate) >= MinShannonBitsPerChar;
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
	/// Matches a whole-string e-mail address (no surrounding whitespace).
	/// </summary>
	[GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
	private static partial Regex EmailRegex();

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
