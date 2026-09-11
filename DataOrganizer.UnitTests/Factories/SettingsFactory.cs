using DataOrganizer.Dto.Settings;
using Material.Colors;
using Material.Styles.Themes.Base;

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
	public static AppSettings CreateSettings(in bool trackHotkeys = false) => new()
	{
		Language = "ja-JP",
		PrimaryColor = PrimaryColor.Red,
		SecondaryColor = SecondaryColor.Red,
		Theme = BaseThemeMode.Dark,
		TrackHotkeys = trackHotkeys
	};
	#endregion
}
