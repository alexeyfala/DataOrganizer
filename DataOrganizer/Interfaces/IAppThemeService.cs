using Material.Colors;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

namespace DataOrganizer.Interfaces;

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
	/// Sets the application theme <see cref="MaterialTheme" />.
	/// </summary>
	void SetTheme(
		BaseThemeMode mode,
		PrimaryColor primaryColor,
		SecondaryColor secondaryColor);
	#endregion
}
