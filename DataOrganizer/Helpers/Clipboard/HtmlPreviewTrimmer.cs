using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Trims blank-rendering markup from the edges of an HTML preview fragment via a real DOM,
/// so leading / trailing empty paragraphs, breaks and whitespace do not push the content
/// out of the fixed-height restore button.
/// </summary>
internal static class HtmlPreviewTrimmer
{
	#region Data
	/// <summary>
	/// Tags that render something even with no text, so a node containing one is never blank.
	/// </summary>
	private const string VisibleEmptySelector = "img, image, svg, hr, table, input, video, audio, canvas, object, iframe, picture";

	/// <inheritdoc cref="HtmlParser" />
	private static readonly HtmlParser Parser = new();
	#endregion

	#region Methods
	/// <summary>
	/// Returns <paramref name="html" /> with leading / trailing blank nodes removed; the input
	/// itself on any parse failure.
	/// </summary>
	public static string Trim(string html)
	{
		try
		{
			IElement body = Parser
				.ParseDocument(html)
				.Body!;

			TrimEdge(body, leading: true);

			TrimEdge(body, leading: false);

			return body.InnerHtml;
		}
		catch
		{
			return html;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when <paramref name="node" /> contributes no visible content (whitespace text,
	/// comment, <c>&lt;br&gt;</c>, or an element whose text is blank and holds no visible element).
	/// </summary>
	private static bool IsBlank(INode node) => node switch
	{
		IText text => string.IsNullOrWhiteSpace(text.Data),
		IComment => true,
		IElement element => element.TagName is "BR"
			|| (element.QuerySelector(VisibleEmptySelector) is null && string.IsNullOrWhiteSpace(element.TextContent)),
		_ => false
	};

	/// <summary>
	/// Drops blank nodes from one end of <paramref name="container" />, then recurses into the
	/// first / last non-blank child so nested leading / trailing blanks are removed too.
	/// </summary>
	private static void TrimEdge(INode container, bool leading)
	{
		while ((leading ? container.FirstChild : container.LastChild) is { } child)
		{
			if (IsBlank(child))
			{
				container.RemoveChild(child);

				continue;
			}

			TrimEdge(child, leading);

			break;
		}
	}
	#endregion
}
