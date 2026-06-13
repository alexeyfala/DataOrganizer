using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Linq;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Wraps occurrences of a search query inside an HTML fragment's text nodes with highlight
/// <c>&lt;span&gt;</c>s, tagging the first match with an id so it can be scrolled into view.
/// </summary>
internal static class HtmlMatchHighlighter
{
	#region Data
	/// <summary>
	/// Inline style applied to each highlighted match.
	/// </summary>
	private const string HighlightStyle = "background-color:#FFE08A;";

	/// <inheritdoc cref="HtmlParser" />
	private static readonly HtmlParser Parser = new();

	/// <summary>
	/// Tags whose text content is not rendered prose and must never be highlighted.
	/// </summary>
	private static readonly string[] SkippedTags = ["STYLE", "SCRIPT"];
	#endregion

	#region Methods
	/// <summary>
	/// Returns <paramref name="html" /> with every case-insensitive occurrence of <paramref name="query" />
	/// wrapped in a highlight span; the first match also gets <paramref name="matchId" /> as its id.
	/// Returns the input unchanged when the query is blank, absent, or on parse failure.
	/// </summary>
	public static string Highlight(string? html, string? query, string matchId)
	{
		if (string.IsNullOrEmpty(html)
			|| string.IsNullOrWhiteSpace(query)
			|| html.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
		{
			return html ?? string.Empty;
		}

		try
		{
			IElement body = Parser
				.ParseDocument(html)
				.Body!;

			bool firstAssigned = false;

			foreach (IText text in body.Descendants<IText>().ToArray())
			{
				if (text.Data.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0
					|| IsInSkippedElement(text))
				{
					continue;
				}

				firstAssigned = WrapMatches(text, query, matchId, firstAssigned);
			}

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
	/// <c>True</c> when <paramref name="text" /> sits inside a tag whose content must not be highlighted.
	/// </summary>
	private static bool IsInSkippedElement(IText text)
	{
		for (INode? node = text.Parent; node is not null; node = node.Parent)
		{
			if (node is IElement element && SkippedTags.Contains(element.TagName))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Replaces <paramref name="text" /> with its segments, each match wrapped in a highlight span
	/// (the first one also carrying <paramref name="matchId" />); returns whether the id is now assigned.
	/// </summary>
	private static bool WrapMatches(
		IText text,
		string query,
		string matchId,
		bool firstAssigned)
	{
		IDocument document = text.Owner!;

		INode parent = text.Parent!;

		string data = text.Data;

		int cursor = 0;

		int index;

		while ((index = data.IndexOf(query, cursor, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			if (index > cursor)
			{
				parent.InsertBefore(document.CreateTextNode(data[cursor..index]), text);
			}

			IElement span = document.CreateElement("span");

			span.SetAttribute("style", HighlightStyle);

			if (!firstAssigned)
			{
				span.SetAttribute("id", matchId);

				firstAssigned = true;
			}

			// Slice the original text to preserve the match's casing.
			span.TextContent = data.Substring(index, query.Length);

			parent.InsertBefore(span, text);

			cursor = index + query.Length;
		}

		if (cursor < data.Length)
		{
			parent.InsertBefore(document.CreateTextNode(data[cursor..]), text);
		}

		parent.RemoveChild(text);

		return firstAssigned;
	}
	#endregion
}
