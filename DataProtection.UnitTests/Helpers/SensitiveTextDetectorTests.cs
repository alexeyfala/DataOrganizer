using AwesomeAssertions;
using DataProtection.Helpers;

namespace DataProtection.UnitTests.Helpers;

[TestFixture(Description = $@"Tests of ""{nameof(SensitiveTextDetector)}"" type")]
internal class SensitiveTextDetectorTests
{
	#region Methods
	/// <summary>
	/// <see cref="SensitiveTextDetector.CountCharacterClasses" />: each of lower case, upper case, digit and other
	/// counts once, and only when it occurs.
	/// </summary>
	[TestCase("abc", 1)]
	[TestCase("ABC", 1)]
	[TestCase("123", 1)]
	[TestCase("#$%", 1)]
	[TestCase("aB", 2)]
	[TestCase("1#", 2)]
	[TestCase("aB1", 3)]
	[TestCase("aB1#", 4)]
	public void CountCharacterClasses_Counts_The_Classes_In_Use(string text, int expected)
	{
		// Act
		int result = SensitiveTextDetector.CountCharacterClasses(text);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LongestSameClassRun" />: neighbours of different classes break a run, so each
	/// class stays apart from the others.
	/// </summary>
	[TestCase("abcde", 5)]
	[TestCase("abcDE", 3)]
	[TestCase("a#", 1)]
	[TestCase("B#", 1)]
	[TestCase("1#", 1)]
	[TestCase("B1#", 1)]
	public void LongestSameClassRun_Measures_The_Longest_Run_Of_One_Class(string text, int expected)
	{
		// Act
		int result = SensitiveTextDetector.LongestSameClassRun(text);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: a value that passes every positive signal is still not a
	/// secret when one negative filter alone catches it (e-mail, hex digest, relative Windows path, home path).
	/// </summary>
	[TestCase("jdoe42@web.de")]
	[TestCase("a1B2c3D4e5F6a7B8c9D0a1B2c3D4e5F6a7B8c9D0")]
	[TestCase(@"Work\Draft7Kq")]
	[TestCase("~/Xk7#pQ2!mZ")]
	public void LooksLikeSecret_Returns_False_For_A_Value_Only_A_Filter_Excludes(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: values caught by the negative
	/// filters (GUID, hex hash, e-mail, URL, path, environment variable) are never secrets.
	/// </summary>
	[TestCase("550e8400-e29b-41d4-a716-446655440000")]
	[TestCase("5d41402abc4b2a76b9719d911017c592")]
	[TestCase("aaf4c61ddcc5e8a2dabede0f3b482cd9aea9434d")]
	[TestCase("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
	[TestCase("user.name@example.com")]
	[TestCase("http://example.com/path")]
	[TestCase("https://sub.example.com/a/b")]
	[TestCase(@"C:\Users\user\AppData\Local")]
	[TestCase("C:/Windows/System32")]
	[TestCase(@"\\server\share\folder")]
	[TestCase("~/Documents/Secret1")]
	[TestCase("%TEMP%aB3cD")]
	public void LooksLikeSecret_Returns_False_For_Excluded_Identifiers(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: a few character classes or low
	/// entropy (e.g. plain words, repeated patterns) are not secrets.
	/// </summary>
	[TestCase("alllowercaseonly")]
	[TestCase("ALLUPPERCASEONLY")]
	[TestCase("Ab1!Ab1!")]
	public void LooksLikeSecret_Returns_False_For_Few_Classes_Or_Low_Entropy(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: null, empty or whitespace-only
	/// input (including interior whitespace) is never a secret.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	[TestCase("aB3# dE5!")]
	public void LooksLikeSecret_Returns_False_For_Null_Empty_Or_Whitespace(string? text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: structured / technical tokens
	/// (paths, versions, identifiers, network addresses, dates, keys) must stay readable.
	/// </summary>
	[TestCase("/usr/local/bin/python3.11")]
	[TestCase("/etc/nginx/sites-enabled/default")]
	[TestCase("src/Main/Program.cs")]
	[TestCase("./build/Output/Release1")]
	[TestCase("~user/Documents/file2.txt")]
	[TestCase("00:1A:2B:3C:4D:5E")]
	[TestCase("2001:db8:85a3::8a2e:370:7334")]
	[TestCase("fe80::1ff:fe23:4567:890a")]
	[TestCase("v1.2.3-rc.1+build.456")]
	[TestCase("1.0.0-alpha.2+exp.sha.5114f85")]
	[TestCase("Microsoft.NET.Sdk.8.0.100")]
	[TestCase("System.Collections.Frozen")]
	[TestCase("MyClass.DoWork(42)")]
	[TestCase("getElementById('main')")]
	[TestCase("public_static_void_Main")]
	[TestCase("2024-06-01T12:30:45Z")]
	[TestCase("2024-06-01T12:30:45.123+03:00")]
	[TestCase("^[A-Za-z0-9]{8,64}$")]
	[TestCase("A1B2C-D3E4F-G5H6I")]
	[TestCase("@john_doe_123")]
	[TestCase("#ThrowbackThursday2024")]
	[TestCase("feature/JIRA-1234-login")]
	[TestCase("0xDEADBEEFCAFE1234")]
	[TestCase("rgba(255,0,128,0.5)")]
	[TestCase("UTF-8/ASCII/CRLF2")]
	[TestCase("Wi-Fi_2.4GHz!")]
	public void LooksLikeSecret_Returns_False_For_Structured_Or_Technical_Tokens(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: tokens longer than the maximum
	/// length are treated as free text, not secrets.
	/// </summary>
	[Test]
	public void LooksLikeSecret_Returns_False_When_Longer_Than_Max()
	{
		// Arrange
		string text = new('x', 65);

		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: tokens shorter than the minimum
	/// length are not treated as secrets.
	/// </summary>
	[TestCase("aB3#")]
	[TestCase("aB3#xY")]
	public void LooksLikeSecret_Returns_False_When_Shorter_Than_Min(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: a value right at a limit is a secret — the shortest and the
	/// longest length, entropy of exactly 3 bits, a run of 4, one separator, and a hash length without hex digits.
	/// </summary>
	[TestCase("aB3#kL9!")]
	[TestCase("aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!")]
	[TestCase("aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!aB3#kL9!")]
	[TestCase("abcdB3#kL9")]
	[TestCase("aB3.kL9!mZ")]
	public void LooksLikeSecret_Returns_True_At_The_Limits_Of_The_Rules(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: bare base64 / JWT fragments have
	/// no separators and no long runs, so they read as random soup. Blurring them is acceptable.
	/// </summary>
	[TestCase("SGVsbG8gV29ybGQh==")]
	[TestCase("eyJhbGciOiJIUzI1NiJ9")]
	public void LooksLikeSecret_Returns_True_For_Base64_Or_Jwt_Tokens(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: high-entropy password-like tokens are detected as secrets.
	/// </summary>
	[TestCase("Xk7#pQ2!mZ")]
	[TestCase("Tr0ub4dor&3")]
	[TestCase("aB3dE5fG7hJ9")]
	[TestCase("P@ssw0rd123!")]
	[TestCase("xQ8!kP3@Lz9#")]
	public void LooksLikeSecret_Returns_True_For_Password_Like_Tokens(string text)
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret(text);

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.LooksLikeSecret" />: surrounding whitespace is trimmed
	/// before evaluation.
	/// </summary>
	[Test]
	public void LooksLikeSecret_Trims_Surrounding_Whitespace()
	{
		// Act
		bool result = SensitiveTextDetector.LooksLikeSecret("  Xk7#pQ2!mZ  ");

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="SensitiveTextDetector.ShannonBitsPerChar" />: the entropy follows how often each character occurs.
	/// </summary>
	[TestCase("aaaa", 0.0)]
	[TestCase("aaaabbbb", 1.0)]
	[TestCase("abcd", 2.0)]
	public void ShannonBitsPerChar_Measures_The_Entropy(string text, double expected)
	{
		// Act
		double result = SensitiveTextDetector.ShannonBitsPerChar(text);

		// Assert
		result
			.Should()
			.BeApproximately(expected, 1e-9);
	}
	#endregion
}
