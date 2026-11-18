using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using DataOrganizer.Documents.TextProcessing.Dto;
using DataOrganizer.Documents.TextProcessing.Helpers;

namespace DataOrganizer.Controls.Text;

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
	/// Resource key of the brush of the bookmarks.
	/// </summary>
	private const string BookmarkBrushKey = "BookmarkBrush";
	#endregion

	#region Methods
	/// <summary>
	/// Builds the runs of <paramref name="text" />, each match of <paramref name="query" /> on the highlight brush.
	/// </summary>
	public static InlineCollection CreateInlines(string text, string? query)
	{
		InlineCollection inlines = [];

		foreach (SearchHighlightSegment segment in SearchHighlight.SplitSegments(text, query))
		{
			Run run = new(segment.Text);

			if (segment.IsMatch)
			{
				run.Background = Brush;
			}

			inlines.Add(run);
		}

		return inlines;
	}

	/// <summary>
	/// Returns the brush of the bookmarks, a color of its own outside the theme.
	/// </summary>
	public static IBrush FindBookmarkBrush(IResourceHost host)
	{
		return host.TryFindResource(BookmarkBrushKey, out object? resource) && resource is IBrush brush
			? brush
			: Brushes.DodgerBlue;
	}
	#endregion
}
