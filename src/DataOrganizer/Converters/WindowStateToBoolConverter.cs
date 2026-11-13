using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

/// <summary>
/// Two-way converter between <see cref="WindowState" /> and the checked state of a maximize button.
/// </summary>
internal sealed class WindowStateToBoolConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is WindowState.Maximized;
	}

	/// <inheritdoc />
	public object ConvertBack(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is true
			? WindowState.Maximized
			: WindowState.Normal;
	}
	#endregion
}
