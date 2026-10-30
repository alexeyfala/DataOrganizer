using Settings.Enums;

namespace Settings.Interfaces;

/// <summary>
/// Applies the Material theme to the application.
/// </summary>
public interface IAppThemeService
{
	#region Methods
	/// <summary>
	/// Applies material theme from the application settings.
	/// </summary>
	void ApplyFromSettings();

	/// <summary>
	/// Sets the theme of the application.
	/// </summary>
	void SetTheme(
		ThemeMode mode,
		ThemePrimaryColor primaryColor,
		ThemeSecondaryColor secondaryColor);
	#endregion
}
