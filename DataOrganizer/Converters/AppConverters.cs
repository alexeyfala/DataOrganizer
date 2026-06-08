using Avalonia.Data.Converters;
using Avalonia.Media;
using DataOrganizer.Enums;
using Entities.Enums;
using Material.Icons;
using Shared.Properties;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters expressed as lambdas, referenced via x:Static.
/// </summary>
internal static class AppConverters
{
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
	#endregion
}
