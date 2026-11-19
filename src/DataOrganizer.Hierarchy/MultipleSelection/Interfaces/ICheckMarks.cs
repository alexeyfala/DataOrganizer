using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.MultipleSelection.Interfaces;

/// <summary>
/// Check marks of the objects of the tree, where a folder is checked together with everything it holds.
/// </summary>
public interface ICheckMarks
{
	#region Methods
	/// <summary>
	/// Checks every object the filter shows with nothing shown inside it, together with everything it holds.
	/// </summary>
	void CheckShown(IEnumerable<ExplorerItemDtoBase> hierarchy, HierarchyFilter filter);

	/// <summary>
	/// Identifiers of the checked objects and of the folders on the way to them.
	/// </summary>
	IReadOnlySet<Guid> GetCheckedWithParents(IEnumerable<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Checked objects that lie in no checked folder.
	/// </summary>
	IReadOnlyList<ExplorerItemDtoBase> GetTopmostChecked(IEnumerable<ExplorerItemDtoBase> hierarchy);

	/// <summary>
	/// Brings a folder whose contents changed, and the folders above it, in line with what they hold.
	/// </summary>
	void Refresh(FolderDto folder);

	/// <summary>
	/// Unchecks a checked object and checks any other one together with everything it holds.
	/// </summary>
	void Toggle(ExplorerItemDtoBase item);

	/// <summary>
	/// Unchecks every object.
	/// </summary>
	void UncheckAll(IEnumerable<ExplorerItemDtoBase> hierarchy);
	#endregion
}
