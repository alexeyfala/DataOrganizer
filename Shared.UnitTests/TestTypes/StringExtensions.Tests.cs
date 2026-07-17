using AwesomeAssertions;
using Shared.Extensions;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(StringExtensions)}"" type")]
internal class StringExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="StringExtensions.IsHtmlColorFormat" />: returns false for null, empty or malformed hex color values.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("#12345")]
	[TestCase("#1234567")]
	[TestCase("#GGGGGG")]
	[TestCase("123456")]
	public void IsHtmlColorFormat_Returns_False_For_Invalid_Value(string? value)
	{
		// Act
		bool result = value.IsHtmlColorFormat();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="StringExtensions.IsHtmlColorFormat" />: returns true for six- and eight-digit hex colors.
	/// </summary>
	[TestCase("#1a2b3c")]
	[TestCase("#1A2B3C4D")]
	public void IsHtmlColorFormat_Returns_True_For_Six_And_Eight_Digit_Hex(string value)
	{
		// Act
		bool result = value.IsHtmlColorFormat();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="StringExtensions.IsUriFormat" />: returns false for null, relative or non-http(s) URIs.
	/// </summary>
	[TestCase(null)]
	[TestCase("")]
	[TestCase("ftp://example.com")]
	[TestCase("/relative/path")]
	[TestCase("not a uri")]
	public void IsUriFormat_Returns_False_For_Non_Http_Or_Relative(string? value)
	{
		// Act
		bool result = value.IsUriFormat();

		// Assert
		result
			.Should()
			.BeFalse();
	}

	/// <summary>
	/// <see cref="StringExtensions.IsUriFormat" />: returns true for absolute http and https URIs.
	/// </summary>
	[TestCase("http://example.com")]
	[TestCase("https://example.com/path?x=1")]
	public void IsUriFormat_Returns_True_For_Http_And_Https(string value)
	{
		// Act
		bool result = value.IsUriFormat();

		// Assert
		result
			.Should()
			.BeTrue();
	}

	/// <summary>
	/// <see cref="StringExtensions.Repeat" />: does not append a trailing separator for a single occurrence.
	/// </summary>
	[Test]
	public void Repeat_Does_Not_Append_Trailing_Separator_For_Single_Count()
	{
		// Act
		string result = "ab".Repeat(1, "-");

		// Assert
		result
			.Should()
			.Be("ab");
	}

	/// <summary>
	/// <see cref="StringExtensions.Repeat" />: places the separator only between occurrences.
	/// </summary>
	[Test]
	public void Repeat_Joins_Value_With_Separator_Between_Occurrences()
	{
		// Act
		string result = "ab".Repeat(3, "-");

		// Assert
		result
			.Should()
			.Be("ab-ab-ab");
	}

	/// <summary>
	/// <see cref="StringExtensions.Repeat" />: returns an empty string for a zero count.
	/// </summary>
	[Test]
	public void Repeat_Returns_Empty_For_Zero_Count()
	{
		// Act
		string result = "ab".Repeat(0, "-");

		// Assert
		result
			.Should()
			.BeEmpty();
	}

	/// <summary>
	/// <see cref="StringExtensions.SurroundWithQuotesIfNeeded" />: leaves an already-quoted value unchanged.
	/// </summary>
	[Test]
	public void SurroundWithQuotesIfNeeded_Leaves_Already_Quoted_Value_Unchanged()
	{
		// Arrange
		const string value = "\"C:\\my path\"";

		// Act
		string result = value.SurroundWithQuotesIfNeeded();

		// Assert
		result
			.Should()
			.Be(value);
	}

	/// <summary>
	/// <see cref="StringExtensions.SurroundWithQuotesIfNeeded" />: wraps an unquoted value in quotes.
	/// </summary>
	[Test]
	public void SurroundWithQuotesIfNeeded_Wraps_Unquoted_Value()
	{
		// Act
		string result = "C:\\my path".SurroundWithQuotesIfNeeded();

		// Assert
		result
			.Should()
			.Be("\"C:\\my path\"");
	}

	/// <summary>
	/// <see cref="StringExtensions.Truncate" />: appends the suffix when the value exceeds the maximum length.
	/// </summary>
	[Test]
	public void Truncate_Appends_Suffix_When_Exceeding_Max()
	{
		// Act
		string result = "hello world".Truncate(5);

		// Assert
		result
			.Should()
			.Be("hello...");
	}

	/// <summary>
	/// <see cref="StringExtensions.Truncate" />: does not append the suffix when the length equals the maximum.
	/// </summary>
	[Test]
	public void Truncate_Does_Not_Append_Suffix_When_Length_Equals_Max()
	{
		// Act
		string result = "hello".Truncate(5);

		// Assert
		result
			.Should()
			.Be("hello");
	}

	/// <summary>
	/// <see cref="StringExtensions.Truncate" />: returns the value unchanged when it is within the maximum length.
	/// </summary>
	[Test]
	public void Truncate_Returns_Value_Unchanged_When_Within_Max()
	{
		// Act
		string result = "hello".Truncate(10);

		// Assert
		result
			.Should()
			.Be("hello");
	}

	/// <summary>
	/// <see cref="StringExtensions.Truncate" />: uses the supplied custom suffix.
	/// </summary>
	[Test]
	public void Truncate_Uses_Custom_Suffix()
	{
		// Act
		string result = "hello world".Truncate(5, "~");

		// Assert
		result
			.Should()
			.Be("hello~");
	}
	#endregion
}
