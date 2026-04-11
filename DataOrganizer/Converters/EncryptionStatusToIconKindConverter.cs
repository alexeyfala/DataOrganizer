using Avalonia.Data.Converters;
using DataOrganizer.Enums;
using Material.Icons;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class EncryptionStatusToIconKindConverter : IValueConverter
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
				EncryptionStatus.Decrypted => MaterialIconKind.LockOpenVariantOutline,
				EncryptionStatus.Encrypted => MaterialIconKind.Lock,
				_ => default
			};
		}

		return default(MaterialIconKind);
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
