using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

using Avalonia.Media;
using Avalonia.Media.Imaging;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Dto.Favorites;
using DataOrganizer.Enums.Documents;
using DataOrganizer.Enums.Encryption;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers.Notes;
using DataOrganizer.Helpers.Text;
using Entities.Enums;
using Material.Icons;
using Repository.Dto;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
	public static FuncValueConverter<int, string> AutoLockDelay { get; } = new(GetAutoLockDelay);

	/// <summary>
	/// <c>True</c> while the auto-lock countdown is about to run out.
	/// </summary>
	public static FuncValueConverter<TimeSpan?, bool> AutoLockIsExpiring { get; } =
		new(remaining => remaining is { } left && left <= AutoLockWarningThreshold);

	/// <summary>
	/// Caption of the time left before the auto-lock.
	/// </summary>
	public static FuncValueConverter<TimeSpan?, string?> AutoLockRemaining { get; } = new(GetAutoLockRemaining);

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
	/// Tooltip of the hotkeys of a file; <c>null</c> for a file without them.
	/// Inputs: [Hotkeys, Hotkeys.Count]; the count only makes the binding read the changed hotkeys again.
	/// </summary>
	public static FuncMultiValueConverter<object?, string?> HotkeysToToolTip { get; } = new(GetHotkeysToolTip);

	/// <summary>
	/// Caption of the line endings of a document; <c>null</c> for a document without line breaks.
	/// </summary>
	public static FuncValueConverter<LineEnding, string?> LineEndingToCaption { get; } =
		new(static ending => ending.ToCaption());

	public static FuncValueConverter<object?, IBrush?> MaterialDesignColorToBrush { get; } =
		new(value => value switch
		{
			ThemePrimaryColor primary => primary.GetBrush(),
			ThemeSecondaryColor secondary => secondary.GetBrush(),
			_ => Brushes.Transparent
		});

	/// <inheritdoc cref="NoteHeaderBuilder.Build" />
	public static FuncValueConverter<string?, string?> NoteHeader { get; } = new(NoteHeaderBuilder.Build);

	/// <summary>
	/// Caption of a number with its digits grouped, put into the format given as the parameter.
	/// </summary>
	public static FuncValueConverter<int, string, string> NumberCaption { get; } = new(GetNumberCaption);

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

	/// <inheritdoc cref="PixelsToGridLengthConverter" />
	public static PixelsToGridLengthConverter PixelsToGridLength { get; } = new();

	/// <summary>
	/// Image of a PNG; <c>null</c> without one.
	/// </summary>
	public static FuncValueConverter<byte[]?, Bitmap?> PngToBitmap { get; } = new(GetBitmap);

	/// <summary>
	/// Image of a PNG decoded at the width given as the parameter; <c>null</c> without one or when it does not decode.
	/// </summary>
	public static FuncValueConverter<byte[]?, double, Bitmap?> PngToPreview { get; } = new(GetPreview);

	/// <summary>
	/// Right gutter for a <c>ScrollViewer</c>, reserved only while content overflows vertically.
	/// Inputs: [Extent.Height, Viewport.Height].
	/// </summary>
	public static FuncMultiValueConverter<double, Thickness> ScrollGutter { get; } = new(GetScrollGutter);

	/// <summary>
	/// Caption of the selection of a document, or of the caret position while nothing is selected.
	/// </summary>
	public static FuncValueConverter<DocumentStatus, string> SelectionOrPosition { get; } = new(GetSelectionOrPosition);

	/// <summary>
	/// New width of a control whose size changed; not a number without the change.
	/// </summary>
	public static FuncValueConverter<SizeChangedEventArgs?, double> SizeChangedToWidth { get; } =
		new(static x => x?.NewSize.Width ?? double.NaN);

	/// <summary>
	/// Name of a language of the syntax highlighting; the one of plain text for <c>null</c> and for a language without a grammar.
	/// </summary>
	public static FuncValueConverter<string?, string> SyntaxLanguageToName { get; } = new(GetSyntaxLanguageName);

	/// <inheritdoc cref="WindowStateToBoolConverter" />
	public static WindowStateToBoolConverter WindowStateToBool { get; } = new();
	#endregion

	#region Helpers
	/// <summary>
	/// Formats a number with its digits grouped the way the current culture groups them.
	/// </summary>
	private static string FormatNumber(int number) => number.ToString("N0", CultureInfo.CurrentCulture);

	/// <summary>
	/// Returns the caption of an auto-lock delay in minutes.
	/// </summary>
	private static string GetAutoLockDelay(int minutes)
	{
		if (minutes <= 0)
		{
			return Strings.Never;
		}

		return string.Format(CultureInfo.CurrentCulture, Strings.MinutesShortFormat, minutes);
	}

	/// <summary>
	/// Returns the caption of the time left before the auto-lock; <c>null</c> while there is no countdown.
	/// </summary>
	private static string? GetAutoLockRemaining(TimeSpan? remaining)
	{
		if (remaining is not { } left)
		{
			return null;
		}

		string time = left.ToString(left.TotalHours >= 1.0 ? @"h\:mm\:ss" : @"mm\:ss", CultureInfo.CurrentCulture);

		return string.Format(CultureInfo.CurrentCulture, Strings.LockedInFormat, time);
	}

	/// <summary>
	/// Returns the image of <paramref name="png" />; <c>null</c> without one.
	/// </summary>
	private static Bitmap? GetBitmap(byte[]? png)
	{
		if (png is null)
		{
			return null;
		}

		using MemoryStream stream = new(png);

		return new Bitmap(stream);
	}

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

	/// <summary>
	/// Returns the tooltip of the hotkeys that come first among the values; <c>null</c> when there are none.
	/// </summary>
	private static string? GetHotkeysToolTip(IEnumerable<object?> values)
	{
		if (values.FirstOrDefault() is not IEnumerable<HotkeyDto> hotkeys)
		{
			return null;
		}

		KeyStroke[] keyStrokes = [.. hotkeys.ToKeyStrokes()];

		if (keyStrokes.Length == 0)
		{
			return null;
		}

		return $"{Strings.Hotkeys}: {keyStrokes.GetHotkeysPresentation()}";
	}

	/// <summary>
	/// Returns a number with its digits grouped, put into a format; the number alone without one.
	/// </summary>
	private static string GetNumberCaption(int number, string? format)
	{
		return string.Format(
			CultureInfo.CurrentCulture,
			format ?? "{0}",
			FormatNumber(number));
	}

	/// <summary>
	/// Returns the image of <paramref name="png" /> decoded at <paramref name="width" />; <c>null</c> without one or when it
	/// does not decode.
	/// </summary>
	private static Bitmap? GetPreview(byte[]? png, double width)
	{
		if (png is not { Length: > 0 })
		{
			return null;
		}

		try
		{
			using MemoryStream stream = new(png);

			// DecodeToWidth keeps aspect ratio and avoids decoding the full-size image.
			return Bitmap.DecodeToWidth(stream, (int)width);
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Returns the right gutter of a scroll viewer: the width of the scroll bar while the content overflows.
	/// </summary>
	private static Thickness GetScrollGutter(IEnumerable<double> values)
	{
		if (values.ToArray() is [double extent, double viewport] && extent > viewport)
		{
			return new Thickness(0.0, 0.0, ScrollBarThickness, 0.0);
		}

		return default;
	}

	/// <summary>
	/// Returns the caption of the selection of a document, or of the caret position while nothing is selected.
	/// </summary>
	private static string GetSelectionOrPosition(DocumentStatus status)
	{
		if (status.SelectionLength > 0)
		{
			return string.Format(
				CultureInfo.CurrentCulture,
				Strings.SelectedFormat,
				FormatNumber(status.SelectionLength),
				FormatNumber(status.SelectionLineCount));
		}

		return string.Format(
			CultureInfo.CurrentCulture,
			Strings.PositionFormat,
			// The position counts from one, like the line and the column.
			FormatNumber(status.CaretOffset + 1));
	}

	/// <summary>
	/// Returns the name of a language of the syntax highlighting; the one of plain text for <c>null</c> and for a
	/// language without a grammar.
	/// </summary>
	private static string GetSyntaxLanguageName(string? language)
	{
		return SyntaxRegistry
			.Instance
			.Languages
			.FirstOrDefault(x => x.Id == language)
			?.Name ?? Strings.PlainText;
	}
	#endregion
}
