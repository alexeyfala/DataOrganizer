using Avalonia.Data.Converters;
using Entities.Enums;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class EntityTypeToExecuteFileMenuItemVisibilityConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (value is EntityType type && type == EntityType.DataSet)
		{
			return false;
		}

		return true;
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
