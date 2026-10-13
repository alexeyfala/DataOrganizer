using DataOrganizer.Dto.Settings;
using DataOrganizer.Enums.Settings;
using System.Globalization;

namespace DataOrganizer.Interfaces.Settings;

/// <summary>
/// Holds the application settings and persists them in a file.
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
		PrimaryColor = ThemePrimaryColor.Teal,
		SecondaryColor = ThemeSecondaryColor.Amber,
		Theme = ThemeMode.Inherit
	};

	/// <summary>
	/// Overwrites <see cref="Settings" /> from <paramref name="value" />.
	/// </summary>
	void Overwrite(AppSettings value);

	/// <summary>
	/// Saves <see cref="Settings" /> in a file.
	/// </summary>
	void Save();
	#endregion
}
