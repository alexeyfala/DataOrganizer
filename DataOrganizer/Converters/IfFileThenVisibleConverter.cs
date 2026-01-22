using Avalonia.Data.Converters;
using Entities.Enums;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class IfFileThenVisibleConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is EntityType type && type == EntityType.File;
	}

	/// <inheritdoc />
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
