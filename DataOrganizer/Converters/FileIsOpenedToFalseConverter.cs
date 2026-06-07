using Avalonia.Data.Converters;
using DataOrganizer.DTO.Entities;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class FileIsOpenedToFalseConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (value is not FileModelDto file)
		{
			return false;
		}

		return !file.IsOpened();
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
