using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Look of the highlighted pieces of text, common to the whole application.
/// </summary>
internal static class TextHighlight
{
	#region Properties
	/// <summary>
	/// Brush behind a highlighted piece of text; the text keeps the color of the theme.
	/// </summary>
	public static IImmutableSolidColorBrush Brush { get; } = new ImmutableSolidColorBrush(Color.FromArgb(
		a: 0x66,
		r: 0xFF,
		g: 0xEB,
		b: 0x3B));
	#endregion
}
