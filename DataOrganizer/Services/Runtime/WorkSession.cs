using DataOrganizer.Dto.Documents;
using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataOrganizer.Services.Runtime;

/// <summary>
/// State of the work that outlives the switch between the main windows and is never written to the settings files.
/// </summary>
internal sealed class WorkSession : ISessionEditorTabs, ISessionHierarchy, ISessionReadOnlyMode, ISessionTreeState
{
	#region Data
	/// <summary>
	/// Identifiers of the objects the tree shows expanded; a set that folders expanded all at once may change in parallel.
	/// </summary>
	private readonly ConcurrentDictionary<Guid, byte> _expandedIds = new();

	/// <summary>
	/// Identifiers of the objects selected in the tree.
	/// </summary>
	private readonly ConcurrentDictionary<Guid, byte> _selectedIds = new();
	#endregion

	#region Properties
	/// <inheritdoc />
	public EditorTabsState? EditorTabs { get; set; }

	/// <inheritdoc />
	public ObservableCollection<ExplorerItemDtoBase> Hierarchy { get; } = [];

	/// <inheritdoc />
	public bool IsReadOnly { get; set; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Clear()
	{
		_expandedIds.Clear();

		_selectedIds.Clear();
	}

	/// <inheritdoc />
	public bool IsExpanded(Guid id) => _expandedIds.ContainsKey(id);

	/// <inheritdoc />
	public bool IsSelected(Guid id) => _selectedIds.ContainsKey(id);

	/// <inheritdoc />
	public void Remember(LoadedHierarchy loaded)
	{
		Mark(_expandedIds, loaded.ExpandedIds);

		Mark(_selectedIds, loaded.SelectedIds);
	}

	/// <inheritdoc />
	public void SetExpanded(Guid id, bool isExpanded) => Set(_expandedIds, id, isExpanded);

	/// <inheritdoc />
	public void SetSelected(Guid id, bool isSelected) => Set(_selectedIds, id, isSelected);
	#endregion

	#region Helpers
	/// <summary>
	/// Adds identifiers to a set.
	/// </summary>
	private static void Mark(ConcurrentDictionary<Guid, byte> set, IEnumerable<Guid> ids)
	{
		foreach (Guid id in ids)
		{
			set[id] = default;
		}
	}

	/// <summary>
	/// Adds an identifier to a set or takes it out.
	/// </summary>
	private static void Set(
		ConcurrentDictionary<Guid, byte> set,
		Guid id,
		bool isMarked)
	{
		if (isMarked)
		{
			set[id] = default;

			return;
		}

		set.TryRemove(id, out _);
	}
	#endregion
}
