using Avalonia.Controls;
using AwesomeAssertions;
using DataOrganizer.Converters;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(WindowStateToBoolConverter)}"" type")]
internal class WindowStateToBoolConverterTests
{
	#region Methods
	/// <summary>
	/// <see cref="WindowStateToBoolConverter.Convert" />: returns false when the value is null.
	/// </summary>
	[Test]
	public void Convert_Returns_False_When_Value_Null()
	{
		// Arrange
		WindowStateToBoolConverter sut = new();

		// Act
		object result = sut.Convert(
			null,
			typeof(bool),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(false);
	}

	/// <summary>
	/// <see cref="WindowStateToBoolConverter.Convert" />: only a maximized window keeps the button checked.
	/// </summary>
	[TestCase(WindowState.Maximized, true)]
	[TestCase(WindowState.Normal, false)]
	[TestCase(WindowState.Minimized, false)]
	[TestCase(WindowState.FullScreen, false)]
	public void Convert_Returns_True_Only_For_A_Maximized_Window(WindowState state, bool expected)
	{
		// Arrange
		WindowStateToBoolConverter sut = new();

		// Act
		object result = sut.Convert(
			state,
			typeof(bool),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(expected);
	}

	/// <summary>
	/// <see cref="WindowStateToBoolConverter.ConvertBack" />: the checked button maximizes the window.
	/// </summary>
	[Test]
	public void ConvertBack_Returns_Maximized_When_Value_True()
	{
		// Arrange
		WindowStateToBoolConverter sut = new();

		// Act
		object result = sut.ConvertBack(
			true,
			typeof(WindowState),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(WindowState.Maximized);
	}

	/// <summary>
	/// <see cref="WindowStateToBoolConverter.ConvertBack" />: the unchecked button brings the window back.
	/// </summary>
	[Test]
	public void ConvertBack_Returns_Normal_When_Value_False()
	{
		// Arrange
		WindowStateToBoolConverter sut = new();

		// Act
		object result = sut.ConvertBack(
			false,
			typeof(WindowState),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(WindowState.Normal);
	}

	/// <summary>
	/// <see cref="WindowStateToBoolConverter.ConvertBack" />: a value that is not a flag brings the window back.
	/// </summary>
	[Test]
	public void ConvertBack_Returns_Normal_When_Value_Null()
	{
		// Arrange
		WindowStateToBoolConverter sut = new();

		// Act
		object result = sut.ConvertBack(
			null,
			typeof(WindowState),
			null,
			CultureInfo.InvariantCulture);

		// Assert
		result
			.Should()
			.Be(WindowState.Normal);
	}
	#endregion
}
