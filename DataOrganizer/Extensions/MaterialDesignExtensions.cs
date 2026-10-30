using Avalonia.Media;
using Material.Colors;
using Material.Styles.Themes.Base;
using Settings.Enums;

namespace DataOrganizer.Extensions;

internal static class MaterialDesignExtensions
{
	#region Methods
	/// <summary>
	/// Returns <see cref="SolidColorBrush" /> according to <see cref="ThemePrimaryColor" />.
	/// </summary>
	public static SolidColorBrush GetBrush(this ThemePrimaryColor color) => new(GetColor(color.ToMaterial()));

	/// <summary>
	/// Returns <see cref="SolidColorBrush" /> according to <see cref="ThemeSecondaryColor" />.
	/// </summary>
	public static SolidColorBrush GetBrush(this ThemeSecondaryColor color) => new(GetColor(color.ToMaterial()));

	/// <summary>
	/// Returns the Material design theme mode of the same name.
	/// </summary>
	public static BaseThemeMode ToMaterial(this ThemeMode mode) => (BaseThemeMode)mode;

	/// <summary>
	/// Returns the Material design primary color of the same name.
	/// </summary>
	public static PrimaryColor ToMaterial(this ThemePrimaryColor color) => (PrimaryColor)color;

	/// <summary>
	/// Returns the Material design secondary color of the same name.
	/// </summary>
	public static SecondaryColor ToMaterial(this ThemeSecondaryColor color) => (SecondaryColor)color;
	#endregion

	#region Helpers
	/// <summary>
	/// Returns <see cref="Color" /> according to <see cref="PrimaryColor" />.
	/// </summary>
	private static Color GetColor(PrimaryColor color) => SwatchHelper.Lookup[(MaterialColor)color];

	/// <summary>
	/// Returns <see cref="Color" /> according to <see cref="SecondaryColor" />.
	/// </summary>
	private static Color GetColor(SecondaryColor color) => SwatchHelper.Lookup[(MaterialColor)color];
	#endregion
}
