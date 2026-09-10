using Material.Colors;
using Material.Styles.Themes.Base;
using System;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Application settings.
/// </summary>
/// <remarks>
/// <see cref="AppSettings" /> is a record for automatic implementation of equality methods.
/// </remarks>
public record AppSettings
{
	#region Properties
	/// <summary>
	/// Minutes of inactivity after which the decrypted contents are hidden; <c>0</c> disables the auto-lock.
	/// </summary>
	public int AutoLockMinutes { get; set; }

	/// <summary>
	/// <c>True</c> when a check for a newer application version runs on startup.
	/// </summary>
	public bool CheckForUpdates { get; set; } = true;

	/// <summary>
	/// Application language.
	/// </summary>
	public required string Language { get; set; }

	/// <summary>
	/// Version already offered to the user, so it is not offered again.
	/// </summary>
	public string? LastNotifiedVersion { get; set; }

	/// <summary>
	/// Timestamp of the last completed update check, in UTC.
	/// </summary>
	public DateTimeOffset? LastUpdateCheckAt { get; set; }

	/// <summary>
	/// <c>True</c> when the clipboard history is kept, encrypted, between sessions.
	/// </summary>
	public bool PersistClipboardHistory { get; set; }

	/// <summary>
	/// Material design primary color.
	/// </summary>
	public required PrimaryColor PrimaryColor { get; set; }

	/// <summary>
	/// Material design secondary color.
	/// </summary>
	public required SecondaryColor SecondaryColor { get; set; }

	/// <summary>
	/// <c>True</c> when the favorites popup opens on hovering over the fix toggle.
	/// </summary>
	public bool ShowFavoritesOnHover { get; set; }

	/// <summary>
	/// Application theme: inherited from the system, light or dark.
	/// </summary>
	public required BaseThemeMode Theme { get; set; }

	/// <summary>
	/// <c>True</c> when the clipboard history is tracked.
	/// </summary>
	public bool TrackClipboardHistory { get; set; }

	/// <summary>
	/// <c>True</c> when hotkeys are tracked.
	/// </summary>
	public bool TrackHotkeys { get; set; }
	#endregion
}
