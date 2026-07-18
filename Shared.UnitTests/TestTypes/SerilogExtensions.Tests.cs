using AwesomeAssertions;
using Serilog.Core;
using Shared.Extensions;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SerilogExtensions)}"" type")]
internal class SerilogExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="SerilogExtensions.DecodeUnicode" />: replaces escaped sequences with their decoded characters.
	/// </summary>
	[Test]
	public void DecodeUnicode_Replaces_Escaped_Sequences_With_Characters()
	{
		// Arrange
		const string value = "\\u0041\\u0042";

		// Act
		string? result = SerilogExtensions.DecodeUnicode(value, Logger.None);

		// Assert
		result
			.Should()
			.Be("AB");
	}

	/// <summary>
	/// <see cref="SerilogExtensions.DecodeUnicode" />: returns <c>null</c> when the value is <c>null</c>.
	/// </summary>
	[Test]
	public void DecodeUnicode_Returns_Null_When_Value_Is_Null()
	{
		// Act
		string? result = SerilogExtensions.DecodeUnicode(null, Logger.None);

		// Assert
		result
			.Should()
			.BeNull();
	}

	/// <summary>
	/// <see cref="SerilogExtensions.DecodeUnicode" />: returns the original value when decoding throws.
	/// </summary>
	[Test]
	public void DecodeUnicode_Returns_Original_Value_When_Decoding_Fails()
	{
		// Arrange
		const string value = @"\uzzzz";

		// Act
		string? result = SerilogExtensions.DecodeUnicode(value, Logger.None);

		// Assert
		result
			.Should()
			.Be(value);
	}

	/// <summary>
	/// <see cref="SerilogExtensions.DecodeUnicode" />: returns the value unchanged when it contains no escape sequences.
	/// </summary>
	[Test]
	public void DecodeUnicode_Returns_Value_Unchanged_When_No_Escapes()
	{
		// Arrange
		const string value = "plain text";

		// Act
		string? result = SerilogExtensions.DecodeUnicode(value, Logger.None);

		// Assert
		result
			.Should()
			.Be(value);
	}
	#endregion
}
