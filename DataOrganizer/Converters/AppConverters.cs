using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DataOrganizer.DTO.Entities;
using DataOrganizer.Enums;
using DataOrganizer.Extensions;
using Entities.Enums;
using Material.Colors;
using Material.Icons;
using Shared.Properties;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters expressed as lambdas, referenced via x:Static.
/// </summary>
internal static class AppConverters
{
	#region Data
	/// <summary>
	/// Material vertical scrollbar thickness; the right gutter reserved while content overflows.
	/// </summary>
	private const double ScrollBarThickness = 10.0;
	#endregion

	#region Properties
	public static FuncValueConverter<EncryptionStatus, IBrush?> EncryptionStatusToIconBrush { get; } =
			new(status => status switch
			{
				EncryptionStatus.Decrypted => Brushes.OrangeRed,
				EncryptionStatus.Encrypted => Brushes.ForestGreen,
				_ => Brushes.Transparent
			});

	public static FuncValueConverter<EncryptionStatus, string?> EncryptionStatusToIconDescription { get; } =
			new(status => status switch
			{
				EncryptionStatus.Decrypted => Strings.ContentIsDecrypted,
				EncryptionStatus.Encrypted => Strings.ContentIsEncrypted,
				_ => null
			});

	public static FuncValueConverter<EncryptionStatus, MaterialIconKind> EncryptionStatusToIconKind { get; } =
			new(status => status switch
			{
				EncryptionStatus.Decrypted => MaterialIconKind.LockOpenVariantOutline,
				EncryptionStatus.Encrypted => MaterialIconKind.Lock,
				_ => default
			});

	public static FuncValueConverter<EntityType, MaterialIconKind> EntityTypeToIconKind { get; } =
			new(type => type switch
			{
				EntityType.Folder => MaterialIconKind.Folder,
				EntityType.File => MaterialIconKind.FileOutline,
				EntityType.DataSet => MaterialIconKind.ViewSplitHorizontal,
				_ => default
			});

	/// <summary>
	/// Two-way enum-to-bool converter for radio-style bindings; the parameter is the enum member to match.
	/// </summary>
	public static EnumToBoolConverter EnumToBool { get; } = new();

	public static FuncValueConverter<FileModelDto, bool> FileIsOpenedToFalse { get; } =
		new(file => file is { } opened && !opened.IsOpened());

	public static FuncValueConverter<object?, IBrush?> MaterialDesignColorToBrush { get; } =
		new(value => value switch
		{
			PrimaryColor primary => primary.GetBrush(),
			SecondaryColor secondary => secondary.GetBrush(),
			_ => Brushes.Transparent
		});

	/// <summary>
	/// Right gutter for a <c>ScrollViewer</c>, reserved only while content overflows vertically.
	/// Inputs: [Extent.Height, Viewport.Height].
	/// </summary>
	public static FuncMultiValueConverter<double, Thickness> ScrollGutter { get; } =
		new(values => values.ToArray() is [double extent, double viewport] && extent > viewport
			? new Thickness(0.0, 0.0, ScrollBarThickness, 0.0)
			: default);
	#endregion
}
