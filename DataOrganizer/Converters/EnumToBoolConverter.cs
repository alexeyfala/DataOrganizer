using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

/// <summary>
/// Two-way converter that is <c>true</c> when the bound enum value equals the converter parameter,
/// and converts a <c>true</c> result back to that parameter — a radio-style enum binding.
/// </summary>
internal sealed class EnumToBoolConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value?.Equals(parameter) == true;
	}

	/// <inheritdoc />
	public object ConvertBack(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		// Only the radio button being checked writes its parameter back; the others do nothing.
		return value is true && parameter is not null
			? parameter
			: BindingOperations.DoNothing;
	}
	#endregion
}
