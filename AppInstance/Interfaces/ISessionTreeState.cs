using Hierarchy.Dto;
using System;

namespace AppInstance.Interfaces;

/// <summary>
/// Objects the tree shows expanded or selected, which the work session keeps while no editor shows the tree.
/// </summary>
public interface ISessionTreeState
{
	#region Methods
	/// <summary>
	/// Forgets every expanded and selected object.
	/// </summary>
	void Clear();

	/// <summary>
	/// <c>True</c> when the tree shows the object expanded.
	/// </summary>
	bool IsExpanded(Guid id);

	/// <summary>
	/// <c>True</c> when the object is selected in the tree.
	/// </summary>
	bool IsSelected(Guid id);

	/// <summary>
	/// Takes the objects stored expanded and selected among the loaded ones.
	/// </summary>
	void Remember(LoadedHierarchy loaded);

	/// <summary>
	/// Marks an object expanded or collapsed.
	/// </summary>
	void SetExpanded(Guid id, bool isExpanded);

	/// <summary>
	/// Marks an object selected or not.
	/// </summary>
	void SetSelected(Guid id, bool isSelected);
	#endregion
}
