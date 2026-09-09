using AwesomeAssertions;
using DataOrganizer.Helpers.Security;
using System;
using System.Text;

namespace DataOrganizer.UnitTests.TestTypes.Security;

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
	/// <see cref="PinnedSecret.IsEmpty" />: set only for a zero-length secret.
	/// </summary>
	[Test]
	public void IsEmpty_Reports_A_Zero_Length_Secret([Values(0, 1, 8)] int length)
	{
		// Arrange
		using PinnedSecret sut = new(length);

		// Act, Assert
		sut.IsEmpty
			.Should()
			.Be(length == 0);
	}

	/// <summary>
	/// <see cref="PinnedSecret.ToUtf8Buffer" />: the characters are encoded as UTF-8, multi-byte ones included.
	/// </summary>
	[Test]
	public void ToUtf8Buffer_Encodes_The_Characters()
	{
		// Arrange
		// Code points keep the source ASCII-only: one 1-byte, one 2-byte and one 3-byte character.
		string value = new([(char)0x31, (char)0xDF, (char)0x43F]);

		using PinnedSecret sut = new(value.Length);

		value.AsSpan().CopyTo(sut.AsSpan());

		// Act
		using PinnedBuffer buffer = sut.ToUtf8Buffer();

		// Assert
		buffer
			.AsReadOnlySpan()
			.ToArray()
			.Should()
			.Equal(Encoding.UTF8.GetBytes(value));
	}

	/// <summary>
	/// <see cref="PinnedSecret.ToUtf8Buffer" />: an empty secret produces an empty buffer.
	/// </summary>
	[Test]
	public void ToUtf8Buffer_Of_An_Empty_Secret_Is_Empty()
	{
		// Arrange
		using PinnedSecret sut = new(0);

		// Act
		using PinnedBuffer buffer = sut.ToUtf8Buffer();

		// Assert
		buffer.Length
			.Should()
			.Be(0);
	}
	#endregion
}
