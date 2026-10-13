using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build application settings.
/// </summary>
public static class SettingsFactory
{
	#region Methods
	/// <summary>
	/// Creates an <see cref="AppSettings" /> object with fixed values.
	/// </summary>
	public static AppSettings CreateSettings(in bool trackHotkeys = false, in int autoLockMinutes = 0) => new()
	{
		AutoLockMinutes = autoLockMinutes,
		Language = "ja-JP",
		PrimaryColor = ThemePrimaryColor.Red,
		SecondaryColor = ThemeSecondaryColor.Red,
		Theme = ThemeMode.Dark,
		TrackHotkeys = trackHotkeys
	};
	#endregion
}
