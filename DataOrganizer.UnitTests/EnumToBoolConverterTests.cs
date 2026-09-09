using Avalonia.Data;
using AwesomeAssertions;
using DataOrganizer.Converters;
using System.Globalization;

namespace DataOrganizer.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(EnumToBoolConverter)}"" type")]
internal class EnumToBoolConverterTests
{
	#region Methods
	/// <summary>
	/// <see cref="EnumToBoolConverter.Convert" />: returns false when the value differs from the parameter.
	/// </summary>
	[Test]
	public void Convert_Returns_False_When_Value_Differs_From_Parameter()
	{
		// Arrange
		EnumToBoolConverter sut = new();

		// Act
		object result = sut.Convert(Sample.A, typeof(bool), Sample.B, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(false);
	}

	/// <summary>
	/// <see cref="EnumToBoolConverter.Convert" />: returns false when the value is null.
	/// </summary>
	[Test]
	public void Convert_Returns_False_When_Value_Null()
	{
		// Arrange
		EnumToBoolConverter sut = new();

		// Act
		object result = sut.Convert(null, typeof(bool), Sample.A, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(false);
	}

	/// <summary>
	/// <see cref="EnumToBoolConverter.Convert" />: returns true when the value equals the parameter.
	/// </summary>
	[Test]
	public void Convert_Returns_True_When_Value_Equals_Parameter()
	{
		// Arrange
		EnumToBoolConverter sut = new();

		// Act
		object result = sut.Convert(Sample.A, typeof(bool), Sample.A, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(true);
	}

	/// <summary>
	/// <see cref="EnumToBoolConverter.ConvertBack" />: returns <see cref="BindingOperations.DoNothing" /> when the parameter is null.
	/// </summary>
	[Test]
	public void ConvertBack_Returns_DoNothing_When_Parameter_Null()
	{
		// Arrange
		EnumToBoolConverter sut = new();

		// Act
		object result = sut.ConvertBack(true, typeof(Sample), null, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(BindingOperations.DoNothing);
	}

	/// <summary>
	/// <see cref="EnumToBoolConverter.ConvertBack" />: returns <see cref="BindingOperations.DoNothing" /> when the value is not true.
	/// </summary>
	[Test]
	public void ConvertBack_Returns_DoNothing_When_Value_False()
	{
		// Arrange
		EnumToBoolConverter sut = new();

		// Act
		object result = sut.ConvertBack(false, typeof(Sample), Sample.A, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(BindingOperations.DoNothing);
	}

	/// <summary>
	/// <see cref="EnumToBoolConverter.ConvertBack" />: returns the parameter when the value is true.
	/// </summary>
	[Test]
	public void ConvertBack_Returns_Parameter_When_Value_True()
	{
		// Arrange
		EnumToBoolConverter sut = new();

		// Act
		object result = sut.ConvertBack(true, typeof(Sample), Sample.A, CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(Sample.A);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Sample enum used as the converter value and parameter.
	/// </summary>
	private enum Sample
	{
		A,
		B
	}
	#endregion
}
