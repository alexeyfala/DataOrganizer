using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.FilteredLists.Dto;

/// <summary>
/// Name filter of the tree: the objects it shows and the folders it opens.
/// </summary>
public sealed class HierarchyFilter
{
	#region Properties
	/// <summary>
	/// Folders on the way to the matched objects.
	/// </summary>
	public required IReadOnlySet<Guid> ExpandedIds { get; init; }

	/// <summary>
	/// <c>True</c> when a search matches no object.
	/// </summary>
	public bool IsNothingFound => Query is not null && ShownIds.Count == 0;

	/// <summary>
	/// Filter that shows every object.
	/// </summary>
	public static HierarchyFilter None { get; } = new()
	{
		ExpandedIds = FrozenSet<Guid>.Empty,
		Query = null,
		ShownIds = FrozenSet<Guid>.Empty
	};

	/// <summary>
	/// Applied search; <c>null</c> while the filter shows every object.
	/// </summary>
	public required string? Query { get; init; }

	/// <summary>
	/// Matched objects with all their parents.
	/// </summary>
	public required IReadOnlySet<Guid> ShownIds { get; init; }
	#endregion

	#region Methods
	/// <summary>
	/// <c>True</c> when the filter shows the object.
	/// </summary>
	public bool Shows(Guid id) => Query is null || ShownIds.Contains(id);
	#endregion
}
