using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

/// <summary>
/// Two-way converter between a width in pixels and an absolute <see cref="GridLength" />.
/// </summary>
internal sealed class PixelsToGridLengthConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is double pixels
			? new GridLength(pixels)
			: BindingOperations.DoNothing;
	}

	/// <inheritdoc />
	public object ConvertBack(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is GridLength { IsAbsolute: true } length
			? length.Value
			: BindingOperations.DoNothing;
	}
	#endregion
}
