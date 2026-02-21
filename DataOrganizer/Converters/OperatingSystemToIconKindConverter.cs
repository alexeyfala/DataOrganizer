using Avalonia.Data.Converters;
using Material.Icons;
using Shared.Common;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class OperatingSystemToIconKindConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object? Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (AppUtils.IsWindows)
		{
			return MaterialIconKind.MicrosoftWindows;
		}

		if (AppUtils.IsLinux)
		{
			return MaterialIconKind.Linux;
		}

		if (AppUtils.IsMacOs)
		{
			return MaterialIconKind.Apple;
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
