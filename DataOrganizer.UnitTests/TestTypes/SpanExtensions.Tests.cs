using AwesomeAssertions;
using DataOrganizer.Extensions;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SpanExtensions)}"" type")]
internal class SpanExtensionsTests
{
	#region Methods
	/// <summary>
	/// <see cref="SpanExtensions.ZeroMemory" />: does not throw for an empty span.
	/// </summary>
	[Test]
	public void ZeroMemory_Does_Nothing_For_Empty_Span()
	{
		// Arrange
		byte[] buffer = [];

		// Act
		Action act = () => buffer
			.AsSpan()
			.ZeroMemory();

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="SpanExtensions.ZeroMemory" />: sets every byte of the span to zero.
	/// </summary>
	[Test]
	public void ZeroMemory_Sets_All_Bytes_To_Zero()
	{
		// Arrange
		byte[] buffer = [1, 2, 3, 4, 5];

		// Act
		buffer
			.AsSpan()
			.ZeroMemory();

		// Assert
		buffer
			.Should()
			.OnlyContain(x => x == 0);
	}
	#endregion
}
