using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// DOM-based touch-ups for an HTML fragment: edge trimming and preformatted normalization.
/// </summary>
internal static partial class HtmlFragmentNormalizer
{
	#region Data
	/// <summary>
	/// Replacement for a tab inside a preformatted block: four non-breaking spaces, since the
	/// HTML engine collapses raw tabs and ordinary spaces.
	/// </summary>
	private const string TabReplacement = "\u00A0\u00A0\u00A0\u00A0";

	/// <summary>
	/// Tags that render something even with no text, so a node containing one is never blank.
	/// </summary>
	private const string VisibleEmptySelector = "img, image, svg, hr, table, input, video, audio, canvas, object, iframe, picture";

	/// <summary>
	/// Characters inside a preformatted block the HTML engine would collapse.
	/// </summary>
	private static readonly char[] CollapsedWhitespace = ['\n', '\r', '\t'];

	/// <inheritdoc cref="HtmlParser" />
	private static readonly HtmlParser Parser = new();
	#endregion

	#region Methods
	/// <summary>
	/// Rewrites <c>rgb()</c> / <c>rgba()</c> colors to plain <c>rgb()</c>, dropping any alpha the
	/// HTML engine would misread as a 0-255 integer and render fully transparent.
	/// </summary>
	public static string NeutralizeUnsupportedColors(string html) => UnsupportedColorRegex().Replace(html, "rgb($1, $2, $3)");

	/// <summary>
	/// Makes <c>&lt;pre&gt;</c> blocks render multi-line (newlines to <c>&lt;br&gt;</c>, tabs to spaces);
	/// returns the input unchanged when there is no such block or on parse failure.
	/// </summary>
	public static string NormalizePreformatted(string html)
	{
		if (!html.Contains("<pre", StringComparison.OrdinalIgnoreCase))
		{
			return html;
		}

		try
		{
			IElement body = Parser
				.ParseDocument(html)
				.Body!;

			foreach (IElement pre in body.QuerySelectorAll("pre"))
			{
				ExpandWhitespace(pre);
			}

			return body.InnerHtml;
		}
		catch
		{
			return html;
		}
	}

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
	/// Replaces, in every text node under <paramref name="pre" />, newlines with <c>&lt;br&gt;</c>
	/// elements and tabs with non-breaking spaces, preserving any nested markup.
	/// </summary>
	private static void ExpandWhitespace(IElement pre)
	{
		IDocument document = pre.Owner!;

		foreach (IText text in pre.Descendants<IText>().ToArray())
		{
			if (text.Data.IndexOfAny(CollapsedWhitespace) < 0)
			{
				continue;
			}

			INode parent = text.Parent!;

			string[] lines = text
				.Data
				.Replace("\r\n", "\n")
				.Replace('\r', '\n')
				.Split('\n');

			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0)
				{
					parent.InsertBefore(document.CreateElement("br"), text);
				}

				string line = lines[i].Replace("\t", TabReplacement);

				if (line.Length > 0)
				{
					parent.InsertBefore(document.CreateTextNode(line), text);
				}
			}

			parent.RemoveChild(text);
		}
	}

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

	/// <summary>
	/// Matches <c>rgb()</c> / <c>rgba()</c> in comma- or space-separated form, capturing the first
	/// three integer channels and discarding any trailing alpha component.
	/// </summary>
	[GeneratedRegex(@"rgba?\(\s*(\d+)\D+(\d+)\D+(\d+)[^)]*\)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex UnsupportedColorRegex();
	#endregion
}
