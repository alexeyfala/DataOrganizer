using DataOrganizer.DTO.Dataset;
using DataOrganizer.Enums;
using System;
using System.Collections.ObjectModel;

namespace DataOrganizer.Helpers;

/// <summary>
/// Moves a <see cref="DatasetRecordBase" /> within a record tree, including across groups.
/// </summary>
internal static class DatasetRecordMoveHelper
{
	#region Methods
	/// <summary>
	/// Finds the collection that directly contains <paramref name="record" />,
	/// searching <paramref name="root" /> and every nested group. Returns <c>null</c> when not found.
	/// </summary>
	public static ObservableCollection<DatasetRecordBase>? FindOwner(
		ObservableCollection<DatasetRecordBase> root,
		DatasetRecordBase record)
	{
		foreach (DatasetRecordBase item in root)
		{
			if (ReferenceEquals(item, record))
			{
				return root;
			}

			if (item is RecordsGroup group && FindOwner(group.Children, record) is { } owner)
			{
				return owner;
			}
		}

		return null;
	}

	/// <summary>
	/// <c>True</c> when <paramref name="target" /> is <paramref name="group" />'s own children
	/// collection or the children of any of its descendant groups.
	/// </summary>
	public static bool IsSelfOrDescendant(
		RecordsGroup group,
		ObservableCollection<DatasetRecordBase> target)
	{
		if (ReferenceEquals(group.Children, target))
		{
			return true;
		}

		foreach (DatasetRecordBase item in group.Children)
		{
			if (item is RecordsGroup child && IsSelfOrDescendant(child, target))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Moves <paramref name="record" /> from <paramref name="source" /> into <paramref name="target" />
	/// at <paramref name="targetIndex" /> (the insertion slot observed before the move). Returns <c>false</c>
	/// when <paramref name="record" /> is absent from <paramref name="source" />.
	/// </summary>
	public static bool Move(
		ObservableCollection<DatasetRecordBase> source,
		DatasetRecordBase record,
		ObservableCollection<DatasetRecordBase> target,
		int targetIndex)
	{
		int oldIndex = source.IndexOf(record);

		if (oldIndex < 0)
		{
			return false;
		}

		if (ReferenceEquals(source, target))
		{
			int newIndex = targetIndex > oldIndex
				? targetIndex - 1
				: targetIndex;

			newIndex = Math.Clamp(newIndex, 0, source.Count - 1);

			if (newIndex != oldIndex)
			{
				source.Move(oldIndex, newIndex);
			}

			return true;
		}

		source.RemoveAt(oldIndex);

		targetIndex = Math.Clamp(targetIndex, 0, target.Count);

		target.Insert(targetIndex, record);

		return true;
	}

	/// <summary>
	/// Resolves the drop target collection, insertion index and placement for a dragged record given
	/// the element under the pointer (<paramref name="targetContext" />) and the pointer's vertical
	/// position within it (<paramref name="pointerYRatio" />, 0 at the top .. 1 at the bottom).
	/// Returns <c>false</c> when the drop is not allowed (onto itself, or a group into itself/a descendant).
	/// </summary>
	public static bool TryResolveTarget(
		ObservableCollection<DatasetRecordBase> root,
		DatasetRecordBase dragged,
		object? targetContext,
		double pointerYRatio,
		out ObservableCollection<DatasetRecordBase> target,
		out int index,
		out DropPlacement placement)
	{
		target = null!;

		index = 0;

		placement = DropPlacement.Into;

		switch (targetContext)
		{
			// Dropping onto a group places the record inside it, at the end.
			case RecordsGroup group:
				if (dragged is RecordsGroup draggedGroup
					&& IsSelfOrDescendant(draggedGroup, group.Children))
				{
					return false;
				}

				target = group.Children;

				index = group.Children.Count;

				return true;

			// Dropping onto a record inserts before or after it, depending on the pointer half.
			case DatasetRecordBase record:
				if (ReferenceEquals(record, dragged)
					|| FindOwner(root, record) is not { } owner
					|| (dragged is RecordsGroup group2
						&& IsSelfOrDescendant(group2, owner)))
				{
					return false;
				}

				placement = pointerYRatio > 0.5
					? DropPlacement.After
					: DropPlacement.Before;

				target = owner;

				index = owner.IndexOf(record) + (placement == DropPlacement.After ? 1 : 0);

				return true;

			// Dropping onto empty surface appends to the root collection.
			default:
				placement = DropPlacement.After;

				target = root;

				index = root.Count;

				return true;
		}
	}
	#endregion
}
