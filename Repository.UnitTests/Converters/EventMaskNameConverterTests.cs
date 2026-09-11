using AwesomeAssertions;
using Repository.Converters;
using SharpHook.Data;
using System;

namespace Repository.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(EventMaskNameConverter)}"" type")]
internal class EventMaskNameConverterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EventMaskNameConverter" />: reads a combination of flags back.
	/// </summary>
	[Test]
	public void Reads_Combined_Flags()
	{
		// Arrange
		EventMaskNameConverter sut = new();

		const EventMask mask = EventMask.LeftCtrl | EventMask.LeftShift;

		// Act
		object? name = sut.ConvertToProvider(mask);

		object? result = sut.ConvertFromProvider(name);

		// Assert
		result
			.Should()
			.Be(mask);
	}

	/// <summary>
	/// <see cref="EventMaskNameConverter" />: every name survives a round trip.
	/// </summary>
	[Test]
	public void Reads_Every_Written_Name()
	{
		// Arrange
		EventMaskNameConverter sut = new();

		foreach (EventMask mask in Enum.GetValues<EventMask>())
		{
			// Act
			object? name = sut.ConvertToProvider(mask);

			object? result = sut.ConvertFromProvider(name);

			// Assert
			result
				.Should()
				.Be(mask);
		}
	}

	/// <summary>
	/// <see cref="EventMaskNameConverter" />: reads a number left by an older format as none.
	/// </summary>
	[Test]
	public void Reads_Number_As_None()
	{
		// Arrange
		EventMaskNameConverter sut = new();

		// Act
		object? result = sut.ConvertFromProvider("2");

		// Assert
		result
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="EventMaskNameConverter" />: reads a name of a flag the library no longer has as none.
	/// </summary>
	[Test]
	public void Reads_Unknown_Name_As_None()
	{
		// Arrange
		EventMaskNameConverter sut = new();

		// Act
		object? result = sut.ConvertFromProvider("LeftHyper");

		// Assert
		result
			.Should()
			.Be(EventMask.None);
	}

	/// <summary>
	/// <see cref="EventMaskNameConverter" />: writes a mask as its name.
	/// </summary>
	[Test]
	public void Writes_Name()
	{
		// Arrange
		EventMaskNameConverter sut = new();

		// Act
		object? result = sut.ConvertToProvider(EventMask.LeftCtrl);

		// Assert
		result
			.Should()
			.Be("LeftCtrl");
	}
	#endregion
}
