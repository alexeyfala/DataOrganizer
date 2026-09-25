using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DataOrganizer.Dto;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Enums;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Notes;
using Entities.Enums;
using Material.Colors;
using Material.Icons;
using Shared.Properties;
using System;
using System.Globalization;
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

	/// <summary>
	/// Time left at which the auto-lock countdown starts warning.
	/// </summary>
	private static readonly TimeSpan AutoLockWarningThreshold = TimeSpan.FromSeconds(20.0);
	#endregion

	#region Properties
	/// <summary>
	/// Caption of an auto-lock delay in minutes, where <c>0</c> stands for no auto-lock.
	/// </summary>
	public static FuncValueConverter<int, string> AutoLockDelay { get; } =
		new(minutes => minutes <= 0
			? Strings.Never
			: string.Format(CultureInfo.CurrentCulture, Strings.MinutesShortFormat, minutes));

	/// <summary>
	/// <c>True</c> while the auto-lock countdown is about to run out.
	/// </summary>
	public static FuncValueConverter<TimeSpan?, bool> AutoLockIsExpiring { get; } =
		new(remaining => remaining is { } left && left <= AutoLockWarningThreshold);

	/// <summary>
	/// Caption of the time left before the auto-lock.
	/// </summary>
	public static FuncValueConverter<TimeSpan?, string?> AutoLockRemaining { get; } =
		new(remaining => remaining is not { } left
			? null
			: string.Format(
				CultureInfo.CurrentCulture,
				Strings.LockedInFormat,
				left.ToString(left.TotalHours >= 1.0 ? @"h\:mm\:ss" : @"mm\:ss", CultureInfo.CurrentCulture)));

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

	public static FuncValueConverter<EntityKind, MaterialIconKind> EntityKindToIconKind { get; } =
		new(kind => kind switch
		{
			EntityKind.Folder => MaterialIconKind.Folder,
			EntityKind.File => MaterialIconKind.FileOutline,
			EntityKind.Dataset => MaterialIconKind.ViewSplitHorizontal,
			_ => default
		});

	/// <inheritdoc cref="EnumToBoolConverter" />
	public static EnumToBoolConverter EnumToBool { get; } = new();

	/// <summary>
	/// <c>True</c> when the folder of a favorites category has a note.
	/// </summary>
	public static FuncValueConverter<FavoriteCategory?, bool> FavoriteCategoryHasNote { get; } =
		new(category => GetFolder(category)?.Note is not null);

	/// <summary>
	/// The folder a favorites category is built from; <c>null</c> for the category of the root objects.
	/// </summary>
	public static FuncValueConverter<FavoriteCategory?, FolderDto?> FavoriteCategoryToFolder { get; } =
		new(GetFolder);

	/// <summary>
	/// Caption of the line endings of a document; <c>null</c> for a document without line breaks.
	/// </summary>
	public static FuncValueConverter<LineEnding, string?> LineEndingToCaption { get; } =
		new(ending => ending switch
		{
			LineEnding.CrLf => "Windows (CR LF)",
			LineEnding.Lf => "Unix (LF)",
			LineEnding.Cr => "Macintosh (CR)",
			LineEnding.Mixed => Strings.MixedLineEndings,
			_ => null
		});

	public static FuncValueConverter<object?, IBrush?> MaterialDesignColorToBrush { get; } =
		new(value => value switch
		{
			PrimaryColor primary => primary.GetBrush(),
			SecondaryColor secondary => secondary.GetBrush(),
			_ => Brushes.Transparent
		});

	/// <inheritdoc cref="NoteHeaderBuilder.Build" />
	public static FuncValueConverter<string?, string?> NoteHeader { get; } = new(NoteHeaderBuilder.Build);

	/// <summary>
	/// Caption of a number with its digits grouped, put into the format given as the parameter.
	/// </summary>
	public static FuncValueConverter<int, string, string> NumberCaption { get; } =
		new((number, format) => string.Format(
			CultureInfo.CurrentCulture,
			format ?? "{0}",
			FormatNumber(number)));

	/// <summary>
	/// Color a password rating is shown in; transparent while there is nothing to rate.
	/// </summary>
	public static FuncValueConverter<PasswordStrength, IBrush?> PasswordStrengthToBrush { get; } =
		new(strength => strength switch
		{
			PasswordStrength.Weak => Brushes.OrangeRed,
			PasswordStrength.Fair => Brushes.DarkGoldenrod,
			PasswordStrength.Strong or PasswordStrength.VeryStrong => Brushes.ForestGreen,
			_ => Brushes.Transparent
		});

	/// <summary>
	/// Caption of a password rating; <c>null</c> while there is nothing to rate.
	/// </summary>
	public static FuncValueConverter<PasswordStrength, string?> PasswordStrengthToCaption { get; } =
		new(strength => strength switch
		{
			PasswordStrength.Weak => Strings.PasswordStrengthWeak,
			PasswordStrength.Fair => Strings.PasswordStrengthFair,
			PasswordStrength.Strong => Strings.PasswordStrengthStrong,
			PasswordStrength.VeryStrong => Strings.PasswordStrengthVeryStrong,
			_ => null
		});

	/// <summary>
	/// Filled part of a password rating bar, out of <see cref="PasswordStrength.VeryStrong" />.
	/// </summary>
	public static FuncValueConverter<PasswordStrength, double> PasswordStrengthToValue { get; } =
		new(strength => (double)strength);

	/// <summary>
	/// Right gutter for a <c>ScrollViewer</c>, reserved only while content overflows vertically.
	/// Inputs: [Extent.Height, Viewport.Height].
	/// </summary>
	public static FuncMultiValueConverter<double, Thickness> ScrollGutter { get; } =
		new(values => values.ToArray() is [double extent, double viewport] && extent > viewport
			? new Thickness(0.0, 0.0, ScrollBarThickness, 0.0)
			: default);

	/// <summary>
	/// Caption of the selection of a document, or of the caret position while nothing is selected.
	/// </summary>
	public static FuncValueConverter<DocumentStatus, string> SelectionOrPosition { get; } =
		new(status => status.SelectionLength > 0
			? string.Format(
				CultureInfo.CurrentCulture,
				Strings.SelectedFormat,
				FormatNumber(status.SelectionLength),
				FormatNumber(status.SelectionLineCount))
			: string.Format(
				CultureInfo.CurrentCulture,
				Strings.PositionFormat,
				// The position counts from one, like the line and the column.
				FormatNumber(status.CaretOffset + 1)));

	/// <inheritdoc cref="WindowStateToBoolConverter" />
	public static WindowStateToBoolConverter WindowStateToBool { get; } = new();
	#endregion

	#region Helpers
	/// <summary>
	/// Formats a number with its digits grouped the way the current culture groups them.
	/// </summary>
	private static string FormatNumber(int number) => number.ToString("N0", CultureInfo.CurrentCulture);

	/// <summary>
	/// The parent folder of the objects of a favorites category; a category always has children.
	/// </summary>
	private static FolderDto? GetFolder(FavoriteCategory? category)
	{
		return category
			?.Children
			.FirstOrDefault()
			?.Parent;
	}
	#endregion
}
