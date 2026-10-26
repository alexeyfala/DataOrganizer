using Avalonia.Data.Converters;
using Avalonia.Media;
using DataProtection.Enums;
using DataProtection.Helpers;
using Hierarchy.Enums;
using Material.Icons;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the protection of the contents: encryption, passwords and the auto-lock,
/// referenced via x:Static.
/// </summary>
internal static class SecurityConverters
{
	#region Data
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

	/// <summary>
	/// Color of the icon of the encryption of an object.
	/// </summary>
	public static FuncValueConverter<EncryptionStatus, IBrush?> EncryptionStatusToIconBrush { get; } =
		new(status => status switch
		{
			EncryptionStatus.Decrypted => Brushes.OrangeRed,
			EncryptionStatus.Encrypted => Brushes.ForestGreen,
			_ => Brushes.Transparent
		});

	/// <summary>
	/// Tip of the icon of the encryption of an object; <c>null</c> for an object that is not protected.
	/// </summary>
	public static FuncValueConverter<EncryptionStatus, string?> EncryptionStatusToIconDescription { get; } =
		new(status => status switch
		{
			EncryptionStatus.Decrypted => Strings.ContentIsDecrypted,
			EncryptionStatus.Encrypted => Strings.ContentIsEncrypted,
			_ => null
		});

	/// <summary>
	/// Icon of the encryption of an object.
	/// </summary>
	public static FuncValueConverter<EncryptionStatus, MaterialIconKind> EncryptionStatusToIconKind { get; } =
		new(status => status switch
		{
			EncryptionStatus.Decrypted => MaterialIconKind.LockOpenVariantOutline,
			EncryptionStatus.Encrypted => MaterialIconKind.Lock,
			_ => default
		});

	/// <summary>
	/// Text shown for a value: the mask while it is hidden, the value itself otherwise. Inputs: [Value, IsHidden].
	/// </summary>
	public static FuncMultiValueConverter<object?, string?> MaskedValue { get; } = new(GetMaskedValue);

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
	#endregion

	#region Helpers
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
	/// Returns the text shown for a value and its hidden state; <c>null</c> until both are known.
	/// </summary>
	private static string? GetMaskedValue(IEnumerable<object?> values)
	{
		return values.ToArray() is [var value, bool isHidden]
			? SecretMask.Apply(value as string, isHidden)
			: null;
	}
	#endregion
}
