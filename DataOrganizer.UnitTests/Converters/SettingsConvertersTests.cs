using Avalonia.Data.Converters;
using Avalonia.Media;
using AwesomeAssertions;
using DataOrganizer.Converters;
using Settings.Enums;
using System.Globalization;

namespace DataOrganizer.UnitTests.Converters;

[TestFixture(Description = $@"Tests of ""{nameof(SettingsConverters)}"" type")]
internal class SettingsConvertersTests
{
	#region Methods
	/// <summary>
	/// <see cref="SettingsConverters.MaterialDesignColorToBrush" />: a primary color is painted with its swatch.
	/// </summary>
	[Test]
	public void MaterialDesignColorToBrush_Paints_A_Primary_Color_With_Its_Swatch()
	{
		// Act
		object? result = Convert(SettingsConverters.MaterialDesignColorToBrush, ThemePrimaryColor.Teal);

		// Assert
		result
			.Should()
			.BeAssignableTo<ISolidColorBrush>()
			.Which
			.Color
			.Should()
			.Be(Color.Parse("#009688"));
	}

	/// <summary>
	/// <see cref="SettingsConverters.MaterialDesignColorToBrush" />: an accent color is painted with its swatch.
	/// </summary>
	[Test]
	public void MaterialDesignColorToBrush_Paints_A_Secondary_Color_With_Its_Swatch()
	{
		// Act
		object? result = Convert(SettingsConverters.MaterialDesignColorToBrush, ThemeSecondaryColor.Amber);

		// Assert
		result
			.Should()
			.BeAssignableTo<ISolidColorBrush>()
			.Which
			.Color
			.Should()
			.Be(Color.Parse("#FFAB00"));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Runs a converter over a color of the theme.
	/// </summary>
	private static object? Convert(FuncValueConverter<object?, IBrush?> converter, object color)
	{
		return converter.Convert(
			color,
			typeof(object),
			null,
			CultureInfo.CurrentCulture);
	}
	#endregion
}
