namespace Settings.Enums;

/// <summary>
/// Theme chosen for the application.
/// </summary>
/// <remarks>
/// The numbers are the ones the settings files keep.
/// </remarks>
public enum ThemeMode
{
	/// <summary>
	/// The theme of the operating system.
	/// </summary>
	Inherit = 0,

	/// <summary>
	/// Dark text on light surfaces.
	/// </summary>
	Light = 1,

	/// <summary>
	/// Light text on dark surfaces.
	/// </summary>
	Dark = 2
}
