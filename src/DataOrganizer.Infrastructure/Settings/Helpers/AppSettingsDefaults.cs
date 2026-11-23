using DataOrganizer.Infrastructure.Settings.Dto;
using DataOrganizer.Infrastructure.Settings.Enums;
using System.Globalization;

namespace DataOrganizer.Infrastructure.Settings.Helpers;

/// <summary>
/// Languages of the application and the default values of its settings.
/// </summary>
public static class AppSettingsDefaults
{
	#region Properties
	/// <summary>
	/// Sequence of application languages.
	/// </summary>
	// Stryker disable once String : a list of languages; a test per entry would copy it
	public static CultureInfo[] Languages { get; } =
	[
		new("en-us"),
		new("ru-ru")
	];
	#endregion

	#region Methods
	/// <summary>
	/// Creates default object of <see cref="AppSettings" />.
	/// </summary>
	// Stryker disable once Conditional,Equality : the language of the installed system cannot be set in a test
	public static AppSettings Create() => new()
	{
		Language = CultureInfo.InstalledUICulture.LCID == 1049 ? Languages[1].Name : Languages[0].Name,
		PrimaryColor = ThemePrimaryColor.Teal,
		SecondaryColor = ThemeSecondaryColor.Amber,
		Theme = ThemeMode.Inherit
	};
	#endregion
}
