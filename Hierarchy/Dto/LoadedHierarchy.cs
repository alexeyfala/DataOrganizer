using System;
using System.Collections.Generic;

namespace Hierarchy.Dto;

/// <summary>
/// Objects of the tree read from the database, with the ones the tree stored expanded and selected.
/// </summary>
public sealed class LoadedHierarchy
{
	#region Properties
	/// <summary>
	/// Nothing read.
	/// </summary>
	public static LoadedHierarchy Empty { get; } = new()
	{
		ExpandedIds = [],
		Items = [],
		SelectedIds = []
	};

	/// <summary>
	/// Identifiers of the folders stored expanded.
	/// </summary>
	public required IReadOnlyCollection<Guid> ExpandedIds { get; init; }

	/// <summary>
	/// Objects at the root of what was read, each with the objects inside.
	/// </summary>
	public required ExplorerItemDtoBase[] Items { get; init; }

	/// <summary>
	/// Identifiers of the objects stored selected.
	/// </summary>
	public required IReadOnlyCollection<Guid> SelectedIds { get; init; }
	#endregion
}
