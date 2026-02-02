using Avalonia.Data.Converters;
using DataOrganizer.Enums;
using Shared.Properties;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class EncryptionStatusToIconDescriptionConverter : IValueConverter
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
				EncryptionStatus.Decrypted => Strings.ContentIsDecrypted,
				EncryptionStatus.Encrypted => Strings.ContentIsEncrypted,
				_ => null
			};
		}

		return null;
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
