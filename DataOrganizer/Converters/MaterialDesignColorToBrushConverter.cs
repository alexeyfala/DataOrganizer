using Avalonia.Data.Converters;
using Avalonia.Media;
using DataOrganizer.Extensions;
using Material.Colors;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class MaterialDesignColorToBrushConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc cref="" />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value switch
		{
			PrimaryColor primary => primary.GetBrush(),
			SecondaryColor secondary => secondary.GetBrush(),
			_ => Brushes.Transparent
		};
	}

	/// <inheritdoc cref="" />
	public object? ConvertBack(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		throw new NotImplementedException();
	}
	#endregion
}
