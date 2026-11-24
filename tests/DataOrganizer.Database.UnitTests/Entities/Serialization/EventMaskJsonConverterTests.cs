using AwesomeAssertions;
using DataOrganizer.Database.Entities.Serialization;
using SharpHook.Data;
using System;
using System.Linq;
using System.Text.Json;

namespace DataOrganizer.Database.UnitTests.Entities.Serialization;

[TestFixture(Description = $@"Tests of ""{nameof(EventMaskJsonConverter)}"" type")]
internal sealed class EventMaskJsonConverterTests
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
		// Act
		EventMask[] misread = [.. Enum
			.GetValues<EventMask>()
			.Where(static x => JsonSerializer.Deserialize<EventMask>(JsonSerializer.Serialize(x, Options), Options) != x)];

		// Assert
		misread
			.Should()
			.BeEmpty();
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
	#endregion
}
