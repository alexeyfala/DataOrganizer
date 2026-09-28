using DataOrganizer.Dto.Clipboard;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Clipboard;

/// <summary>
/// Splits text into ordered plain / matched segments around a search query.
/// </summary>
internal static class SearchHighlight
{
	#region Methods
	/// <summary>
	/// Splits <paramref name="text" /> into ordered segments, flagging each case-insensitive
	/// occurrence of <paramref name="query" />. A blank query yields a single plain segment.
	/// </summary>
	public static IReadOnlyList<SearchHighlightSegment> SplitSegments(string? text, string? query)
	{
		if (string.IsNullOrEmpty(text))
		{
			return [];
		}

		if (string.IsNullOrEmpty(query))
		{
			return [new SearchHighlightSegment(text, IsMatch: false)];
		}

		List<SearchHighlightSegment> segments = [];

		int index = 0;

		while (index < text.Length)
		{
			int matchStart = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase);

			if (matchStart < 0)
			{
				segments.Add(new SearchHighlightSegment(text[index..], IsMatch: false));

				break;
			}

			if (matchStart > index)
			{
				segments.Add(new SearchHighlightSegment(text[index..matchStart], IsMatch: false));
			}

			segments.Add(new SearchHighlightSegment(text.Substring(matchStart, query.Length), IsMatch: true));

			index = matchStart + query.Length;
		}

		return segments;
	}
	#endregion
}
