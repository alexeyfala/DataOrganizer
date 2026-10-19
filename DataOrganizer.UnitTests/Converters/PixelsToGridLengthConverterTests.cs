using Avalonia.Controls;
using Avalonia.Data;
using AwesomeAssertions;
using DataOrganizer.Converters;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(PixelsToGridLengthConverter)}"" type")]
internal class PixelsToGridLengthConverterTests
{
	#region Methods
	/// <summary>
	/// <see cref="PixelsToGridLengthConverter.Convert" />: a width in pixels becomes an absolute length.
	/// </summary>
	[Test]
	public void Convert_Gives_An_Absolute_Length()
	{
		// Arrange
		PixelsToGridLengthConverter sut = new();

		// Act
		object result = sut.Convert(
			150.0,
			typeof(GridLength),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(new GridLength(150.0, GridUnitType.Pixel));
	}

	/// <summary>
	/// <see cref="PixelsToGridLengthConverter.Convert" />: a value that is no width leaves the length alone.
	/// </summary>
	[Test]
	public void Convert_Leaves_A_Value_That_Is_No_Width()
	{
		// Arrange
		PixelsToGridLengthConverter sut = new();

		// Act
		object result = sut.Convert(
			null,
			typeof(GridLength),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(BindingOperations.DoNothing);
	}

	/// <summary>
	/// <see cref="PixelsToGridLengthConverter.ConvertBack" />: an absolute length gives its width in pixels.
	/// </summary>
	[Test]
	public void ConvertBack_Gives_The_Width_Of_An_Absolute_Length()
	{
		// Arrange
		PixelsToGridLengthConverter sut = new();

		// Act
		object result = sut.ConvertBack(
			new GridLength(222.0),
			typeof(double),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(222.0);
	}

	/// <summary>
	/// <see cref="PixelsToGridLengthConverter.ConvertBack" />: a length that is not absolute leaves the width alone.
	/// </summary>
	[TestCase(GridUnitType.Auto)]
	[TestCase(GridUnitType.Star)]
	public void ConvertBack_Leaves_The_Width_Of_A_Relative_Length(GridUnitType unit)
	{
		// Arrange
		PixelsToGridLengthConverter sut = new();

		// Act
		object result = sut.ConvertBack(
			new GridLength(1.0, unit),
			typeof(double),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(BindingOperations.DoNothing);
	}
	#endregion
}
