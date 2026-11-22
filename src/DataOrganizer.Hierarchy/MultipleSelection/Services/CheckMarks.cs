using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Extensions;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using DataOrganizer.Hierarchy.MultipleSelection.Interfaces;
using DataOrganizer.Hierarchy.MultipleSelection.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Hierarchy.MultipleSelection.Services;

/// <inheritdoc cref="ICheckMarks" />
public sealed class CheckMarks : ICheckMarks
{
	#region Data
	/// <inheritdoc cref="IMessenger" />
	private readonly IMessenger _messenger;

	/// <inheritdoc cref="ISessionTreeState" />
	private readonly ISessionTreeState _treeState;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="CheckMarks" /> class.
	/// </summary>
	public CheckMarks(IMessenger messenger, ISessionTreeState treeState)
	{
		_messenger = messenger;

		_treeState = treeState;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void CheckShown(IEnumerable<ExplorerItemDtoBase> hierarchy, HierarchyFilter filter)
	{
		HashSet<Guid> changed = [];

		CheckShownIn(
			hierarchy,
			filter,
			changed);

		Notify(changed);
	}

	/// <inheritdoc />
	public IReadOnlySet<Guid> GetCheckedWithParents(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		return hierarchy
			.Flatten()
			.Where(x => _treeState.IsChecked(x.Id) != false)
			.Select(static x => x.Id)
			.ToHashSet();
	}

	/// <inheritdoc />
	public IReadOnlyList<ExplorerItemDtoBase> GetTopmostChecked(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		List<ExplorerItemDtoBase> result = [];

		AddTopmostChecked(hierarchy, result);

		return result;
	}

	/// <inheritdoc />
	public void Refresh(FolderDto folder)
	{
		HashSet<Guid> changed = [];

		if (Set(
			folder,
			GetState(folder),
			changed))
		{
			UpdateParents(folder, changed);
		}

		Notify(changed);
	}

	/// <inheritdoc />
	public void Toggle(ExplorerItemDtoBase item)
	{
		HashSet<Guid> changed = [];

		SetWhole(
			item,
			_treeState.IsChecked(item.Id) != true,
			changed);

		UpdateParents(item, changed);

		Notify(changed);
	}

	/// <inheritdoc />
	public void UncheckAll(IEnumerable<ExplorerItemDtoBase> hierarchy)
	{
		HashSet<Guid> changed = [];

		foreach (ExplorerItemDtoBase item in hierarchy.Flatten())
		{
			_ = Set(
				item,
				false,
				changed);
		}

		Notify(changed);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds the checked objects that lie in no checked folder.
	/// </summary>
	private void AddTopmostChecked(IEnumerable<ExplorerItemDtoBase> items, List<ExplorerItemDtoBase> result)
	{
		foreach (ExplorerItemDtoBase item in items)
		{
			switch (_treeState.IsChecked(item.Id))
			{
				case true:
					result.Add(item);
					break;
				case null when item is FolderDto folder:
					AddTopmostChecked(folder.Children, result);
					break;
			}
		}
	}

	/// <summary>
	/// Checks the shown objects with nothing shown inside them, then brings each folder that shows more in line with
	/// what it holds.
	/// </summary>
	private void CheckShownIn(
		IEnumerable<ExplorerItemDtoBase> items,
		HierarchyFilter filter,
		HashSet<Guid> changed)
	{
		foreach (ExplorerItemDtoBase item in items.Where(x => filter.Shows(x.Id)))
		{
			if (item is not FolderDto folder || !folder.Children.Any(x => filter.Shows(x.Id)))
			{
				SetWhole(
					item,
					true,
					changed);

				continue;
			}

			CheckShownIn(
				folder.Children,
				filter,
				changed);

			_ = Set(
				folder,
				GetState(folder),
				changed);
		}
	}

	/// <summary>
	/// The state of a folder that follows from the states of the objects inside it; an empty folder keeps a whole check
	/// and loses a partial one.
	/// </summary>
	private bool? GetState(FolderDto folder)
	{
		if (folder.Children.Count == 0)
		{
			return _treeState.IsChecked(folder.Id) == true;
		}

		bool?[] states =
		[
			.. folder.Children
				.Select(x => _treeState.IsChecked(x.Id))
				.Distinct()
		];

		return states.Length == 1 ? states[0] : null;
	}

	/// <summary>
	/// Reports the objects whose state changed.
	/// </summary>
	private void Notify(HashSet<Guid> changed)
	{
		if (changed.Count == 0)
		{
			return;
		}

		_messenger.Send(new CheckMarksChangedMessage(changed));
	}

	/// <summary>
	/// Gives an object a state; <c>false</c> when the object had it already.
	/// </summary>
	private bool Set(
		ExplorerItemDtoBase item,
		bool? state,
		HashSet<Guid> changed)
	{
		if (_treeState.IsChecked(item.Id) == state)
		{
			return false;
		}

		_treeState.SetChecked(item.Id, state);

		changed.Add(item.Id);

		return true;
	}

	/// <summary>
	/// Gives an object and everything it holds the same state.
	/// </summary>
	private void SetWhole(
		ExplorerItemDtoBase item,
		bool isChecked,
		HashSet<Guid> changed)
	{
		_ = Set(
			item,
			isChecked,
			changed);

		if (item is not FolderDto folder)
		{
			return;
		}

		foreach (ExplorerItemDtoBase child in folder.GetAllChildren())
		{
			_ = Set(
				child,
				isChecked,
				changed);
		}
	}

	/// <summary>
	/// Brings the folders on the way to an object in line with what they hold, up to the first one whose state stays.
	/// </summary>
	private void UpdateParents(ExplorerItemDtoBase item, HashSet<Guid> changed)
	{
		for (FolderDto? parent = item.Parent; parent is not null; parent = parent.Parent)
		{
			if (!Set(
				parent,
				GetState(parent),
				changed))
			{
				return;
			}
		}
	}
	#endregion
}
