using Avalonia.Data.Converters;
using Entities.Enums;
using Material.Icons;
using System;
using System.Globalization;

namespace DataOrganizer.Converters;

internal sealed class EntityTypeToMaterialIconKindConverter : IValueConverter
{
	#region Methods
	/// <inheritdoc />
	public object Convert(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		if (value is EntityType type)
		{
			return type switch
			{
				EntityType.Folder => MaterialIconKind.Folder,
				EntityType.File => MaterialIconKind.FileOutline,
				EntityType.DataSet => MaterialIconKind.ViewSplitHorizontal,
				_ => throw new NotImplementedException()
			};
		}

		return default(MaterialIconKind);
	}

	/// <inheritdoc />
	public object ConvertBack(
		object? value,
		Type targetType,
		object? parameter,
		CultureInfo culture)
	{
		throw new NotImplementedException();
	}
	#endregion
}
