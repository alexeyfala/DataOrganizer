using Avalonia.Data.Converters;
using DataOrganizer.Enums;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class CloseAllExecutedFilesButtonVisibilityConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (value is EditorRightSideSheetContentType type && type == EditorRightSideSheetContentType.ExecutedFiles)
		{
			return true;
		}

		return false;
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
