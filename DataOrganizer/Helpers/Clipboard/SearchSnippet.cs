using System;
using System.Text.RegularExpressions;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Builds a compact, single-line excerpt of text centered on the first match of a search query.
/// </summary>
internal static partial class SearchSnippet
{
	#region Data
	/// <summary>
	/// Horizontal ellipsis marking text trimmed off either side of the excerpt.
	/// </summary>
	private const string Ellipsis = "…";

	/// <summary>
	/// Characters kept before the match so it does not sit flush against the left edge.
	/// </summary>
	private const int LeadingContextLength = 24;

	/// <summary>
	/// Nominal excerpt length; the whole match is always kept even when it is longer.
	/// </summary>
	private const int WindowLength = 240;
	#endregion

	#region Methods
	/// <summary>
	/// Returns <paramref name="text" /> with whitespace collapsed and excerpted around the first
	/// case-insensitive occurrence of <paramref name="query" />. A blank or unmatched query yields
	/// the full collapsed text.
	/// </summary>
	public static string Build(
		string? text,
		string? query,
		int leadingContext = LeadingContextLength,
		int windowLength = WindowLength)
	{
		string collapsed = CollapseWhitespace(text);

		if (collapsed.Length == 0 || string.IsNullOrWhiteSpace(query))
		{
			return collapsed;
		}

		int matchStart = collapsed.IndexOf(query, StringComparison.OrdinalIgnoreCase);

		if (matchStart < 0)
		{
			return collapsed;
		}

		int start = Math.Max(0, matchStart - leadingContext);

		int matchEnd = matchStart + query.Length;

		// Never cut the match itself, even when it is longer than the nominal window.
		int end = Math.Min(collapsed.Length, Math.Max(start + windowLength, matchEnd));

		ReadOnlySpan<char> window = collapsed.AsSpan(start, end - start);

		ReadOnlySpan<char> prefix = start > 0 ? Ellipsis : string.Empty;

		ReadOnlySpan<char> suffix = end < collapsed.Length ? Ellipsis : string.Empty;

		return string.Concat(prefix, window, suffix);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Collapses every run of whitespace to a single space and trims the ends.
	/// </summary>
	private static string CollapseWhitespace(string? text)
	{
		return string.IsNullOrEmpty(text)
			? string.Empty
			: WhitespaceRegex().Replace(text, " ").Trim();
	}

	/// <summary>
	/// Matches any run of one or more whitespace characters.
	/// </summary>
	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
	#endregion
}
