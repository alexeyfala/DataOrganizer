using DataOrganizer.Enums;
using System;

namespace DataOrganizer.Helpers.Security;

/// <summary>
/// Rates a password by the bits of guessing it is estimated to cost, without copying the password.
/// </summary>
internal static class PasswordStrengthEstimator
{
	#region Data
	/// <summary>
	/// Bits credited to a common word, however long it is: it is guessed as a whole.
	/// </summary>
	private const double CommonWordBits = 11.0;

	/// <summary>
	/// Size of the pool a digit is drawn from.
	/// </summary>
	private const int DigitPoolSize = 10;

	/// <summary>
	/// Least bits of <see cref="PasswordStrength.Fair" />.
	/// </summary>
	private const double FairBits = 40.0;

	/// <summary>
	/// Size of the pool a letter of one case is drawn from.
	/// </summary>
	private const int LetterPoolSize = 26;

	/// <summary>
	/// Size of the pool a character beyond ASCII is drawn from.
	/// </summary>
	private const int NonAsciiPoolSize = 100;

	/// <summary>
	/// Weight of a character repeating the one before it.
	/// </summary>
	private const double RepeatedCharWeight = 0.25;

	/// <summary>
	/// Weight of a character continuing an ascending or descending run.
	/// </summary>
	private const double SequenceCharWeight = 0.5;

	/// <summary>
	/// Least bits of <see cref="PasswordStrength.Strong" />.
	/// </summary>
	private const double StrongBits = 60.0;

	/// <summary>
	/// Size of the pool an ASCII character other than a letter or a digit is drawn from.
	/// </summary>
	private const int SymbolPoolSize = 33;

	/// <summary>
	/// Least bits of <see cref="PasswordStrength.VeryStrong" />.
	/// </summary>
	private const double VeryStrongBits = 80.0;

	/// <summary>
	/// Words every leaked-credential list opens with; they are tried before anything else.
	/// </summary>
	private static readonly string[] _commonWords =
	[
		"000000",
		"111111",
		"123123",
		"123456",
		"1234567",
		"12345678",
		"123456789",
		"1234567890",
		"abc123",
		"admin",
		"azerty",
		"baseball",
		"batman",
		"computer",
		"dragon",
		"flower",
		"football",
		"freedom",
		"google",
		"hello",
		"iloveyou",
		"jordan",
		"letmein",
		"login",
		"loveme",
		"master",
		"michael",
		"monkey",
		"ninja",
		"passwort",
		"password",
		"photoshop",
		"princess",
		"qazwsx",
		"qwerty",
		"qwertz",
		"samsung",
		"secret",
		"shadow",
		"starwars",
		"sunshine",
		"superman",
		"trustno1",
		"welcome",
		"whatever",
		"zaq12wsx"
	];
	#endregion

	#region Methods
	/// <summary>
	/// Rates <paramref name="password" />; <see cref="PasswordStrength.None" /> while it is empty.
	/// </summary>
	public static PasswordStrength Estimate(ReadOnlySpan<char> password)
	{
		if (password.IsWhiteSpace())
		{
			return PasswordStrength.None;
		}

		double bitsPerChar = Math.Log2(AlphabetSize(password));

		double bits = EffectiveLength(password) * bitsPerChar;

		int commonLength = LongestCommonWordLength(password);

		if (commonLength > 0)
		{
			// The known part is guessed in one step, so its characters stop paying for themselves.
			bits = Math.Max(0.0, bits - (commonLength * bitsPerChar)) + CommonWordBits;
		}

		return bits switch
		{
			>= VeryStrongBits => PasswordStrength.VeryStrong,
			>= StrongBits => PasswordStrength.Strong,
			>= FairBits => PasswordStrength.Fair,
			_ => PasswordStrength.Weak
		};
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Number of characters an attacker has to try per position, from the classes in use.
	/// </summary>
	private static int AlphabetSize(ReadOnlySpan<char> password)
	{
		bool hasLower = false;

		bool hasUpper = false;

		bool hasDigit = false;

		bool hasSymbol = false;

		bool hasNonAscii = false;

		foreach (char c in password)
		{
			if (char.IsAsciiLetterLower(c))
			{
				hasLower = true;
			}
			else if (char.IsAsciiLetterUpper(c))
			{
				hasUpper = true;
			}
			else if (char.IsAsciiDigit(c))
			{
				hasDigit = true;
			}
			else if (char.IsAscii(c))
			{
				hasSymbol = true;
			}
			else
			{
				hasNonAscii = true;
			}
		}

		return (hasLower ? LetterPoolSize : 0)
			+ (hasUpper ? LetterPoolSize : 0)
			+ (hasDigit ? DigitPoolSize : 0)
			+ (hasSymbol ? SymbolPoolSize : 0)
			+ (hasNonAscii ? NonAsciiPoolSize : 0);
	}

	/// <summary>
	/// Length <paramref name="password" /> is worth once repeated characters and runs such as
	/// <c>abcd</c> are discounted, as those cost an attacker far less than a fresh character.
	/// </summary>
	private static double EffectiveLength(ReadOnlySpan<char> password)
	{
		double length = 0.0;

		for (int i = 0; i < password.Length; i++)
		{
			char current = password[i];

			char previous = i > 0 ? password[i - 1] : default;

			if (i == 0)
			{
				length++;
			}
			else if (current == previous)
			{
				length += RepeatedCharWeight;
			}
			else if (IsSequential(previous, current))
			{
				length += SequenceCharWeight;
			}
			else
			{
				length++;
			}
		}

		return length;
	}

	/// <summary>
	/// <c>True</c> when the two letters or digits are neighbours in their alphabet.
	/// </summary>
	private static bool IsSequential(char previous, char current)
	{
		return char.IsAsciiLetterOrDigit(previous)
			&& char.IsAsciiLetterOrDigit(current)
			&& (current == previous + 1 || current == previous - 1);
	}

	/// <summary>
	/// Length of the longest common word <paramref name="password" /> is built around; <c>0</c> for none.
	/// </summary>
	private static int LongestCommonWordLength(ReadOnlySpan<char> password)
	{
		int longest = 0;

		foreach (string word in _commonWords)
		{
			if (word.Length > longest && password.Contains(word, StringComparison.OrdinalIgnoreCase))
			{
				longest = word.Length;
			}
		}

		return longest;
	}
	#endregion
}
