using DataOrganizer.DTO.Settings;
using Material.Colors;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;
using System.Globalization;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Manages application settings.
/// </summary>
public interface IAppSettingsManager
{
	#region Properties
	/// <summary>
	/// Sequence of application languages.
	/// </summary>
	public static CultureInfo[] Languages { get; } =
	[
		new("en-US"),
		new("ru-RU")
	];

	/// <inheritdoc cref="AppSettings" />
	AppSettings Settings { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Creates default object of <see cref="AppSettings" />.
	/// </summary>
	public static AppSettings CreateDefaultSettings() => new()
	{
		Language = Languages[0].Name,
		PrimaryColor = PrimaryColor.Teal,
		SecondaryColor = SecondaryColor.Amber,
		Theme = BaseThemeMode.Inherit
	};

	/// <summary>
	/// Applies material theme from <see cref="Settings" />.
	/// </summary>
	void ApplyMeterialTheme();

	/// <summary>
	/// Overwrites <see cref="Settings" /> from <paramref name="value"/>.
	/// </summary>
	void OverwriteSettings(AppSettings value);

	/// <summary>
	/// Saves <see cref="Settings" /> in file.
	/// </summary>
	void SaveSettingsInFile();

	/// <summary>
	/// Sets the application theme <see cref="MaterialTheme" />.
	/// </summary>
	void SetAppMaterialTheme(
		in BaseThemeMode mode,
		in PrimaryColor primaryColor,
		in SecondaryColor secondaryColor);
	#endregion
}
