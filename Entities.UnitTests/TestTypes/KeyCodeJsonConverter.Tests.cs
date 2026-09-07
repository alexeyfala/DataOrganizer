using AwesomeAssertions;
using Entities.Converters;
using SharpHook.Data;
using System;
using System.Text.Json;

namespace Entities.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(KeyCodeJsonConverter)}"" type")]
internal class KeyCodeJsonConverterTests
{
	#region Data
	/// <summary>
	/// Options holding the converter under test.
	/// </summary>
	private static readonly JsonSerializerOptions Options = new()
	{
		Converters = { new KeyCodeJsonConverter() }
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="KeyCodeJsonConverter" />: every name survives a round trip.
	/// </summary>
	[Test]
	public void Reads_Every_Written_Name()
	{
		foreach (KeyCode code in Enum.GetValues<KeyCode>())
		{
			// Act
			string json = JsonSerializer.Serialize(code, Options);

			KeyCode result = JsonSerializer.Deserialize<KeyCode>(json, Options);

			// Assert
			result
				.Should()
				.Be(code);
		}
	}

	/// <summary>
	/// <see cref="KeyCodeJsonConverter" />: reads a known name back.
	/// </summary>
	[Test]
	public void Reads_Known_Name()
	{
		// Act
		KeyCode result = JsonSerializer.Deserialize<KeyCode>(@"""VcA""", Options);

		// Assert
		result
			.Should()
			.Be(KeyCode.VcA);
	}

	/// <summary>
	/// <see cref="KeyCodeJsonConverter" />: reads a number written as text as undefined.
	/// </summary>
	[Test]
	public void Reads_Number_As_Text_As_Undefined()
	{
		// Act
		KeyCode result = JsonSerializer.Deserialize<KeyCode>(@"""9999""", Options);

		// Assert
		result
			.Should()
			.Be(KeyCode.VcUndefined);
	}

	/// <summary>
	/// <see cref="KeyCodeJsonConverter" />: reads a number left by an older format as undefined.
	/// </summary>
	[Test]
	public void Reads_Number_As_Undefined()
	{
		// Act
		KeyCode result = JsonSerializer.Deserialize<KeyCode>("65", Options);

		// Assert
		result
			.Should()
			.Be(KeyCode.VcUndefined);
	}

	/// <summary>
	/// <see cref="KeyCodeJsonConverter" />: reads a name of a key the library no longer has as undefined.
	/// </summary>
	[Test]
	public void Reads_Unknown_Name_As_Undefined()
	{
		// Act
		KeyCode result = JsonSerializer.Deserialize<KeyCode>(@"""VcKanji""", Options);

		// Assert
		result
			.Should()
			.Be(KeyCode.VcUndefined);
	}

	/// <summary>
	/// <see cref="KeyCodeJsonConverter" />: writes a key code as its name.
	/// </summary>
	[Test]
	public void Writes_Name()
	{
		// Act
		string result = JsonSerializer.Serialize(KeyCode.VcA, Options);

		// Assert
		result
			.Should()
			.Be(@"""VcA""");
	}
	#endregion
}
