using AwesomeAssertions;
using DataOrganizer.Helpers;
using System;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(PinnedSecret)}"" type")]
internal class PinnedSecretTests
{
	#region Methods
	/// <summary>
	/// <see cref="PinnedSecret.AsSpan" /> + <see cref="PinnedSecret.AsReadOnlySpan" />: data written through the span round-trips when read back through the read-only span.
	/// </summary>
	[Test]
	public void AsSpan_Allows_Writing_And_Reading_Through_AsReadOnlySpan()
	{
		// Arrange
		using PinnedSecret sut = new(5);

		// Act
		"abcde".AsSpan().CopyTo(sut.AsSpan());

		ReadOnlySpan<char> roundTrip = sut.AsReadOnlySpan();

		// Assert
		roundTrip
			.ToArray()
			.Should()
			.BeEquivalentTo(['a', 'b', 'c', 'd', 'e']);
	}

	/// <summary>
	/// <see cref="PinnedSecret.Dispose" />: calling Dispose twice does not throw.
	/// </summary>
	[Test]
	public void Dispose_Is_Idempotent()
	{
		// Arrange
		PinnedSecret sut = new(3);

		// Act
		Action act = () =>
		{
			sut.Dispose();

			sut.Dispose();
		};

		// Assert
		act
			.Should()
			.NotThrow();
	}

	/// <summary>
	/// <see cref="PinnedSecret.Dispose" />: disposing zeroes the underlying buffer.
	/// </summary>
	[Test]
	public void Dispose_Zeroes_The_Buffer()
	{
		// Arrange
		PinnedSecret sut = new(4);

		"keys".AsSpan().CopyTo(sut.AsSpan());

		sut.AsReadOnlySpan().ToArray()
			.Should()
			.NotContain('\0');

		// Act
		sut.Dispose();

		// Assert
		sut.AsReadOnlySpan().ToArray()
			.Should()
			.OnlyContain(c => c == '\0');
	}

	/// <summary>
	/// <see cref="PinnedSecret.Length" />: the length reflects the size passed to the constructor.
	/// </summary>
	[Test]
	public void Length_Reflects_Constructor_Argument()
	{
		// Arrange + Act
		using PinnedSecret sut = new(7);

		// Assert
		sut.Length
			.Should()
			.Be(7);
	}
	#endregion
}
