using Avalonia.Data.Converters;
using DataOrganizer.Enums;
using Shared.Extensions;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class EncryptionStatusToIconVisibilityConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		return value is EncryptionStatus status && status.IsNotDefault();
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
