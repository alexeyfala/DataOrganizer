using DataOrganizer.Dto.Documents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Search over the items of a list with a search, such as the languages or the encodings of a text.
/// </summary>
internal static class ChoiceSearch
{
	#region Methods
	/// <summary>
	/// Returns the items that match a search, the best matches first and the items that cannot be chosen last.
	/// </summary>
	public static SelectorChoice[] Find(IEnumerable<SelectorChoice> choices, string search)
	{
		return [.. choices
			.Select(x => (Choice: x, Rank: Rank(x, search)))
			.Where(static x => x.Rank is not null)
			.OrderBy(static x => !x.Choice.IsAvailable)
			.ThenBy(static x => x.Rank)
			.Select(static x => x.Choice)];
	}

	/// <summary>
	/// Returns the search a typed text stands for; empty when nothing is searched.
	/// </summary>
	public static string Normalize(string? typed)
	{
		// An extension may be typed with its dot.
		return typed?.Trim().TrimStart('.') ?? string.Empty;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns how well an item matches the search, where a lower rank is better; <c>null</c> when it does not match.
	/// </summary>
	private static int? Rank(SelectorChoice choice, string search)
	{
		if (choice
			.SearchTerms
			.Any(x => x.Equals(search, StringComparison.OrdinalIgnoreCase)))
		{
			return 0;
		}

		if (choice
			.Name
			.StartsWith(search, StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (choice
			.SearchTerms
			.Any(x => x.StartsWith(search, StringComparison.OrdinalIgnoreCase))
			|| choice.Description?.StartsWith(search, StringComparison.OrdinalIgnoreCase) == true)
		{
			return 2;
		}

		if (choice
			.Name
			.Contains(search, StringComparison.OrdinalIgnoreCase))
		{
			return 3;
		}

		return choice.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ? 4 : null;
	}
	#endregion
}
