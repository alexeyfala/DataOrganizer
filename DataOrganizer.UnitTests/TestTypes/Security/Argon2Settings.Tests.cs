using AwesomeAssertions;
using DataOrganizer.Helpers.Security;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace DataOrganizer.UnitTests.TestTypes.Security;

[TestFixture(Description = $@"Tests of ""{nameof(Argon2Settings)}"" type")]
internal class Argon2SettingsTests
{
	#region Methods
	/// <summary>
	/// <see cref="Argon2Settings.Read" />: a layout shorter than the written one is refused.
	/// </summary>
	[Test]
	public void Read_Rejects_A_Truncated_Layout()
	{
		// Arrange
		byte[] header = new byte[Argon2Settings.HeaderSize - 1];

		// Act
		Action act = () => Argon2Settings.Read(header);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="Argon2Settings.Read" />: values steering an allocation are bounded.
	/// </summary>
	[TestCase(0u, 3, 1, Description = "No memory at all")]
	[TestCase(1024u, 3, 1, Description = "Less memory than supported")]
	[TestCase(uint.MaxValue, 3, 1, Description = "More memory than supported")]
	[TestCase(65536u, 0, 1, Description = "No pass at all")]
	[TestCase(65536u, 255, 1, Description = "More passes than supported")]
	[TestCase(65536u, 3, 0, Description = "No lane at all")]
	[TestCase(65536u, 3, 255, Description = "More lanes than supported")]
	public void Read_Rejects_Unsupported_Values(
		uint memorySize,
		byte numberOfPasses,
		byte degreeOfParallelism)
	{
		// Arrange
		byte[] header = new byte[Argon2Settings.HeaderSize];

		BinaryPrimitives.WriteUInt32LittleEndian(header, memorySize);

		header[4] = numberOfPasses;

		header[5] = degreeOfParallelism;

		// Act
		Action act = () => Argon2Settings.Read(header);

		// Assert
		act
			.Should()
			.ThrowExactly<CryptographicException>();
	}

	/// <summary>
	/// <see cref="Argon2Settings.Read" />: the written values come back unchanged.
	/// </summary>
	[Test]
	public void Read_Returns_The_Written_Values()
	{
		// Arrange
		Argon2Settings settings = Argon2Settings.Current;

		byte[] header = new byte[Argon2Settings.HeaderSize];

		// Act
		settings.Write(header);

		// Assert
		Argon2Settings
			.Read(header)
			.Should()
			.Be(settings);
	}
	#endregion
}
