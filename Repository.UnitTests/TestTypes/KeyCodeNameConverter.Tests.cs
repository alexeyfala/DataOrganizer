using AwesomeAssertions;
using Repository.Converters;
using SharpHook.Data;
using System;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(KeyCodeNameConverter)}"" type")]
internal class KeyCodeNameConverterTests
{
	#region Methods
	/// <summary>
	/// <see cref="KeyCodeNameConverter" />: every name survives a round trip.
	/// </summary>
	[Test]
	public void Reads_Every_Written_Name()
	{
		// Arrange
		KeyCodeNameConverter sut = new();

		foreach (KeyCode code in Enum.GetValues<KeyCode>())
		{
			// Act
			object? name = sut.ConvertToProvider(code);

			object? result = sut.ConvertFromProvider(name);

			// Assert
			result
				.Should()
				.Be(code);
		}
	}

	/// <summary>
	/// <see cref="KeyCodeNameConverter" />: reads a known name back.
	/// </summary>
	[Test]
	public void Reads_Known_Name()
	{
		// Arrange
		KeyCodeNameConverter sut = new();

		// Act
		object? result = sut.ConvertFromProvider("VcA");

		// Assert
		result
			.Should()
			.Be(KeyCode.VcA);
	}

	/// <summary>
	/// <see cref="KeyCodeNameConverter" />: reads a number left by an older format as undefined.
	/// </summary>
	[Test]
	public void Reads_Number_As_Undefined()
	{
		// Arrange
		KeyCodeNameConverter sut = new();

		// Act
		object? result = sut.ConvertFromProvider("65");

		// Assert
		result
			.Should()
			.Be(KeyCode.VcUndefined);
	}

	/// <summary>
	/// <see cref="KeyCodeNameConverter" />: reads a name of a key the library no longer has as undefined.
	/// </summary>
	[Test]
	public void Reads_Unknown_Name_As_Undefined()
	{
		// Arrange
		KeyCodeNameConverter sut = new();

		// Act
		object? result = sut.ConvertFromProvider("VcKanji");

		// Assert
		result
			.Should()
			.Be(KeyCode.VcUndefined);
	}

	/// <summary>
	/// <see cref="KeyCodeNameConverter" />: writes a key code as its name.
	/// </summary>
	[Test]
	public void Writes_Name()
	{
		// Arrange
		KeyCodeNameConverter sut = new();

		// Act
		object? result = sut.ConvertToProvider(KeyCode.VcA);

		// Assert
		result
			.Should()
			.Be("VcA");
	}
	#endregion
}
