using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Hierarchy.UnitTests.MultipleSelection.TestHelpers.Fakes;

/// <summary>
/// Tree state that keeps only the check marks, in memory.
/// </summary>
internal sealed class CheckMarksTreeState : ISessionTreeState
{
	#region Data
	/// <summary>
	/// States of the objects by their identifiers.
	/// </summary>
	private readonly Dictionary<Guid, bool?> _checkStates = [];
	#endregion

	#region Methods
	/// <inheritdoc />
	public bool? IsChecked(Guid id) => _checkStates.GetValueOrDefault(id, false);

	/// <inheritdoc />
	public void SetChecked(Guid id, bool? isChecked) => _checkStates[id] = isChecked;
	#endregion

	#region Unused
	/// <inheritdoc />
	public void Clear() => throw new NotSupportedException();

	/// <inheritdoc />
	public bool IsExpanded(Guid id) => throw new NotSupportedException();

	/// <inheritdoc />
	public bool IsSelected(Guid id) => throw new NotSupportedException();

	/// <inheritdoc />
	public void Remember(LoadedHierarchy loaded) => throw new NotSupportedException();

	/// <inheritdoc />
	public void SetExpanded(Guid id, bool isExpanded) => throw new NotSupportedException();

	/// <inheritdoc />
	public void SetSelected(Guid id, bool isSelected) => throw new NotSupportedException();
	#endregion
}
