using DataOrganizer.DTO.Settings;
using Material.Colors;
using Material.Styles.Themes.Base;
using System.Globalization;

namespace DataOrganizer.Interfaces.Settings;

/// <summary>
/// Holds the application settings and persists them in file.
/// </summary>
public interface IAppSettingsStore
{
	#region Properties
	/// <summary>
	/// Sequence of application languages.
	/// </summary>
	public static CultureInfo[] Languages { get; } =
	[
		new("en-us"),
		new("ru-ru")
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
		Language = CultureInfo.InstalledUICulture.LCID == 1049 ? Languages[1].Name : Languages[0].Name,
		PrimaryColor = PrimaryColor.Teal,
		SecondaryColor = SecondaryColor.Amber,
		Theme = BaseThemeMode.Inherit
	};

	/// <summary>
	/// Overwrites <see cref="Settings" /> from <paramref name="value" />.
	/// </summary>
	void Overwrite(AppSettings value);

	/// <summary>
	/// Saves <see cref="Settings" /> in file.
	/// </summary>
	void Save();
	#endregion
}
