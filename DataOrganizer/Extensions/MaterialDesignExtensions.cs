using Avalonia.Media;
using Material.Colors;

namespace DataOrganizer.Extensions;

internal static class MaterialDesignExtensions
{
	#region Methods
	/// <summary>
	/// Returns <see cref="SolidColorBrush" /> according to <see cref="PrimaryColor" />.
	/// </summary>
	public static SolidColorBrush GetBrush(this PrimaryColor color) => new(GetColor(color));

	/// <summary>
	/// Returns <see cref="SolidColorBrush" /> according to <see cref="SecondaryColor" />.
	/// </summary>
	public static SolidColorBrush GetBrush(this SecondaryColor color) => new(GetColor(color));
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
