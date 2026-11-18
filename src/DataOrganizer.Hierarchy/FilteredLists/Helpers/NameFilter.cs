using DataOrganizer.Hierarchy.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace DataOrganizer.Hierarchy.FilteredLists.Helpers;

/// <summary>
/// Rule by which a search matches the names of objects.
/// </summary>
public static class NameFilter
{
	#region Methods
	/// <summary>
	/// Returns the predicate of a search: a blank one matches every object, any other the names that hold it, case aside.
	/// </summary>
	public static Func<INamed, bool> Create(string? query)
	{
		return IsBlank(query)
			? static _ => true
			: x => x.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// <c>True</c> when a search holds nothing to look for.
	/// </summary>
	public static bool IsBlank([NotNullWhen(false)] string? query) => string.IsNullOrWhiteSpace(query);
	#endregion
}
