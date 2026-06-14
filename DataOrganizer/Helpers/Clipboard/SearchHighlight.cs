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
	public static IReadOnlyList<Segment> SplitSegments(string? text, string? query)
	{
		if (string.IsNullOrEmpty(text))
		{
			return [];
		}

		if (string.IsNullOrEmpty(query))
		{
			return [new Segment(text, IsMatch: false)];
		}

		List<Segment> segments = [];

		int index = 0;

		while (index < text.Length)
		{
			int matchStart = text.IndexOf(query, index, StringComparison.OrdinalIgnoreCase);

			if (matchStart < 0)
			{
				segments.Add(new Segment(text[index..], IsMatch: false));

				break;
			}

			if (matchStart > index)
			{
				segments.Add(new Segment(text[index..matchStart], IsMatch: false));
			}

			segments.Add(new Segment(text.Substring(matchStart, query.Length), IsMatch: true));

			index = matchStart + query.Length;
		}

		return segments;
	}
	#endregion

	#region Types
	/// <summary>
	/// A contiguous run of text flagged as a query match or as plain text.
	/// </summary>
	internal readonly record struct Segment(string Text, bool IsMatch);
	#endregion
}
