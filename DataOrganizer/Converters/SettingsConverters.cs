using Avalonia.Data.Converters;
using Avalonia.Media;
using DataOrganizer.Enums.Settings;
using DataOrganizer.Extensions.Settings;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the settings, referenced via x:Static.
/// </summary>
internal static class SettingsConverters
{
	#region Properties
	/// <summary>
	/// Color of a primary or secondary color of the theme; transparent for any other value.
	/// </summary>
	public static FuncValueConverter<object?, IBrush?> MaterialDesignColorToBrush { get; } =
		new(value => value switch
		{
			ThemePrimaryColor primary => primary.GetBrush(),
			ThemeSecondaryColor secondary => secondary.GetBrush(),
			_ => Brushes.Transparent
		});
	#endregion
}
