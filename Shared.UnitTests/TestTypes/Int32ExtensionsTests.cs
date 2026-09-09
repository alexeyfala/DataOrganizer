using AwesomeAssertions;
using Shared.Extensions;

namespace Shared.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(Int32Extensions)}"" type")]
internal class Int32ExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="Int32Extensions.TakeDigits" />: returns the leading digits, the whole value when fewer
	/// digits are requested than present, zero for zero and the absolute value for negatives.
	/// </summary>
	[TestCase(0, 3, 0)]
	[TestCase(12345, 2, 12)]
	[TestCase(12345, 5, 12345)]
	[TestCase(42, 5, 42)]
	[TestCase(42, 2, 42)]
	[TestCase(-12345, 3, 123)]
	public void TakeDigits_Returns_Expected_Leading_Digits(int value, int count, int expected)
	{
		// Act
		int result = value.TakeDigits(count);

		// Assert
		result
			.Should()
			.Be(expected);
	}
	#endregion
}
