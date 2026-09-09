using AwesomeAssertions;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Security;
using System;
using System.Linq;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(PinnedBuffer)}"" type")]
internal class PinnedBufferTests
{
	#region Methods
	/// <summary>
	/// <see cref="PinnedBuffer.AsSpan" /> + <see cref="PinnedBuffer.AsReadOnlySpan" />: data written through the span round-trips when read back through the read-only span.
	/// </summary>
	[Test]
	public void AsSpan_Allows_Writing_And_Reading_Through_AsReadOnlySpan()
	{
		// Arrange
		byte[] expected = CreatePattern(32);

		using PinnedBuffer sut = new(expected.Length);

		// Act
		expected.CopyTo(sut.AsSpan());

		ReadOnlySpan<byte> roundTrip = sut.AsReadOnlySpan();

		// Assert
		roundTrip
			.ToArray()
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// The copying constructor takes a snapshot of the source, so wiping the source leaves the buffer intact.
	/// </summary>
	[Test]
	public void Constructor_Copies_The_Source()
	{
		// Arrange
		byte[] source = CreatePattern(24);

		byte[] expected = CreatePattern(24);

		// Act
		using PinnedBuffer sut = new(source);

		source.ZeroMemory();

		// Assert
		sut
			.Length
			.Should()
			.Be(expected.Length);

		sut.AsReadOnlySpan()
			.ToArray()
			.Should()
			.Equal(expected);
	}

	/// <summary>
	/// <see cref="PinnedBuffer.Dispose" />: calling Dispose twice does not throw.
	/// </summary>
	[Test]
	public void Dispose_Is_Idempotent()
	{
		// Arrange
		PinnedBuffer sut = new(3);

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
	/// <see cref="PinnedBuffer.Dispose" />: disposing zeroes the underlying buffer.
	/// </summary>
	[Test]
	public void Dispose_Zeroes_The_Buffer()
	{
		// Arrange
		PinnedBuffer sut = new(CreatePattern(16));

		sut.AsReadOnlySpan()
			.ToArray()
			.Should()
			.NotContain(0);

		// Act
		sut.Dispose();

		// Assert
		sut.AsReadOnlySpan()
			.ToArray()
			.Should()
			.OnlyContain(b => b == 0);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Builds a deterministic buffer free of zero bytes, so wiping is observable.
	/// </summary>
	private static byte[] CreatePattern(int length)
	{
		return [.. Enumerable
			.Range(1, length)
			.Select(value => (byte)value)];
	}
	#endregion
}
