using Avalonia.Data.Converters;
using DataOrganizer.Enums;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class CloseAllExecutingFilesButtonVisibilityConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is RightSideSheetContentType type && type == RightSideSheetContentType.ExecutingFiles;
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
