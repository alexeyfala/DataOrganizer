using AwesomeAssertions;
using Entities.Serialization;
using SharpHook.Data;
using System;
using System.Text.Json;

namespace Entities.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EventMaskJsonConverter)}"" type")]
internal class EventMaskJsonConverterTests
{
	#region Data
	/// <summary>
	/// Options holding the converter under test.
	/// </summary>
	private static readonly JsonSerializerOptions Options = new()
	{
		Converters = { new EventMaskJsonConverter() }
	};
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="EventMaskJsonConverter" />: a mask of several flags survives a round trip.
	/// </summary>
	[Test]
	public void Reads_Combined_Mask()
	{
		// Arrange
		const EventMask mask = EventMask.LeftCtrl | EventMask.LeftShift;

		// Act
		string json = JsonSerializer.Serialize(mask, Options);

		EventMask result = JsonSerializer.Deserialize<EventMask>(json, Options);

		// Assert
		result
			.Should()
			.Be(mask);
	}

	/// <summary>
	/// <see cref="EventMaskJsonConverter" />: every name survives a round trip.
	/// </summary>
	[Test]
	public void Reads_Every_Written_Name()
	{
		foreach (EventMask mask in Enum.GetValues<EventMask>())
		{
			// Act
			string json = JsonSerializer.Serialize(mask, Options);

			EventMask result = JsonSerializer.Deserialize<EventMask>(json, Options);

			// Assert
			result
				.Should()
				.Be(mask);
		}
	}

	/// <summary>
	/// <see cref="EventMaskJsonConverter" />: reads a known name back.
	/// </summary>
	[Test]
	public void Reads_Known_Name()
	{
		// Act
		EventMask result = JsonSerializer.Deserialize<EventMask>(@"""LeftCtrl""", Options);

		// Assert
		result
			.Should()
			.Be(EventMask.LeftCtrl);
	}

	/// <summary>
	/// <see cref="EventMaskJsonConverter" />: reads a number left by an older format as no mask.
	/// </summary>
	[Test]
	public void Reads_Number_As_None()
	{
		// Act
		EventMask result = JsonSerializer.Deserialize<EventMask>("1", Options);

		// Assert
		result
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="EventMaskJsonConverter" />: reads a name the library no longer has as no mask.
	/// </summary>
	[Test]
	public void Reads_Unknown_Name_As_None()
	{
		// Act
		EventMask result = JsonSerializer.Deserialize<EventMask>(@"""LeftHyper""", Options);

		// Assert
		result
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="EventMaskJsonConverter" />: writes a mask as its name.
	/// </summary>
	[Test]
	public void Writes_Name()
	{
		// Act
		string result = JsonSerializer.Serialize(EventMask.LeftCtrl, Options);

		// Assert
		result
			.Should()
			.Be(@"""LeftCtrl""");
	}
	#endregion
}
