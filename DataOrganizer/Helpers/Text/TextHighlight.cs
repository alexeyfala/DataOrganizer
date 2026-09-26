using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Look of the highlighted pieces of text and of the bookmarks, common to the whole application.
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

	/// <summary>
	/// Brush of the marks that show the highlighted pieces of text along the scroll range.
	/// </summary>
	public static IImmutableSolidColorBrush MarkBrush { get; } = new ImmutableSolidColorBrush(Color.FromRgb(
		r: 0xFB,
		g: 0xC0,
		b: 0x2D));
	#endregion

	#region Data
	/// <summary>
	/// Resource key of the primary brush of the theme.
	/// </summary>
	private const string BookmarkBrushKey = "MaterialPrimaryMidBrush";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the brush of the bookmarks, the primary brush of the theme.
	/// </summary>
	public static IBrush FindBookmarkBrush(IResourceHost host)
	{
		return host.TryFindResource(BookmarkBrushKey, out object? resource) && resource is IBrush brush
			? brush
			: Brushes.DodgerBlue;
	}
	#endregion
}
