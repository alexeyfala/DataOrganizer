using Avalonia.Data.Converters;
using Avalonia.Media;
using DataOrganizer.Enums;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class EncryptionStatusToIconBrushConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (value is EncryptionStatus status)
		{
			return status switch
			{
				EncryptionStatus.Decrypted => Brushes.OrangeRed,
				EncryptionStatus.Encrypted => Brushes.ForestGreen,
				_ => Brushes.Transparent
			};
		}

		return Brushes.Transparent;
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
