using DataOrganizer.Enums.Dataset;
using DataOrganizer.Models.Dataset;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.Helpers.Dataset;

/// <summary>
/// Rules of the record tree of a dataset: adding, deleting, moving and sorting records and finding the collection that
/// holds a record.
/// </summary>
internal static class DatasetRecordTree
{
	#region Methods
	/// <summary>
	/// Adds <paramref name="record" /> at the end of <paramref name="group" />, which it expands, or at the end of
	/// <paramref name="records" /> without a group.
	/// </summary>
	public static void Add(
		Collection<DatasetRecordBase> records,
		DatasetRecordBase record,
		RecordsGroup? group)
	{
		if (group is null)
		{
			records.Add(record);

			return;
		}

		group.IsExpanded = true;

		group
			.Children
			.Add(record);
	}

	/// <summary>
	/// Deletes <paramref name="record" /> from <paramref name="records" /> or from a group at any depth.
	/// </summary>
	public static void Delete(
		Collection<DatasetRecordBase> records,
		DatasetRecordBase record)
	{
		foreach (DatasetRecordBase item in records)
		{
			if (ReferenceEquals(item, record) && records.Remove(record))
			{
				return;
			}

			if (item is RecordsGroup group && group.Children.Count > 0)
			{
				Delete(group.Children, record);
			}
		}
	}

	/// <summary>
	/// Finds the collection that directly holds <paramref name="record" />, searching <paramref name="records" /> and every
	/// nested group; <c>null</c> when the record is not there.
	/// </summary>
	public static ObservableCollection<DatasetRecordBase>? FindOwner(
		ObservableCollection<DatasetRecordBase> records,
		DatasetRecordBase record)
	{
		foreach (DatasetRecordBase item in records)
		{
			if (ReferenceEquals(item, record))
			{
				return records;
			}

			if (item is RecordsGroup group && FindOwner(group.Children, record) is { } owner)
			{
				return owner;
			}
		}

		return null;
	}

	/// <summary>
	/// Lists <paramref name="records" /> together with the records of every nested group.
	/// </summary>
	public static IEnumerable<DatasetRecordBase> Flatten(IEnumerable<DatasetRecordBase> records)
	{
		Stack<DatasetRecordBase> stack = new(records);

		while (stack.Count > 0)
		{
			DatasetRecordBase item = stack.Pop();

			yield return item;

			if (item is RecordsGroup group)
			{
				foreach (DatasetRecordBase child in group.Children)
				{
					stack.Push(child);
				}
			}
		}
	}

	/// <summary>
	/// Moves <paramref name="record" /> of <paramref name="records" /> into <paramref name="target" /> at
	/// <paramref name="targetIndex" />, the insertion slot seen before the move; <c>false</c> when the record is not there
	/// or stays where it is.
	/// </summary>
	public static bool Move(
		ObservableCollection<DatasetRecordBase> records,
		DatasetRecordBase record,
		ObservableCollection<DatasetRecordBase> target,
		int targetIndex)
	{
		if (FindOwner(records, record) is not { } source)
		{
			return false;
		}

		int oldIndex = source.IndexOf(record);

		if (ReferenceEquals(source, target))
		{
			int newIndex = targetIndex > oldIndex
				? targetIndex - 1
				: targetIndex;

			newIndex = Math.Clamp(newIndex, 0, source.Count - 1);

			// Dropping onto its own edge leaves the record where it is.
			if (newIndex == oldIndex)
			{
				return false;
			}

			source.Move(oldIndex, newIndex);

			return true;
		}

		source.RemoveAt(oldIndex);

		targetIndex = Math.Clamp(targetIndex, 0, target.Count);

		target.Insert(targetIndex, record);

		return true;
	}

	/// <summary>
	/// Sorts <paramref name="records" /> and the records of their groups in <paramref name="direction" />: groups first, then
	/// key and value records, then value records; <c>false</c> when the sort would lose records.
	/// </summary>
	public static bool Sort(
		ObservableCollection<DatasetRecordBase> records,
		ListSortDirection direction)
	{
		int beforeCount = GetCount(records);

		DatasetRecordBase[] sorted = SortRecords(records, direction);

		int afterCount = GetCount(sorted);

		if (afterCount != beforeCount)
		{
			return false;
		}

		records.Clear();

		records.AddRange(sorted);

		return true;
	}

	/// <summary>
	/// Finds where <paramref name="dragged" /> lands when dropped over <paramref name="targetContext" /> (<c>null</c> over
	/// empty space) at <paramref name="pointerYRatio" /> of its height; <c>false</c> when the drop is not allowed.
	/// </summary>
	public static bool TryResolveTarget(
		ObservableCollection<DatasetRecordBase> records,
		DatasetRecordBase dragged,
		DatasetRecordBase? targetContext,
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
					|| FindOwner(records, record) is not { } owner
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

				target = records;

				index = records.Count;

				return true;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Counts <paramref name="records" /> together with the records of every nested group.
	/// </summary>
	private static int GetCount(IEnumerable<DatasetRecordBase> records) => Flatten(records).Count();

	/// <summary>
	/// <c>True</c> when <paramref name="target" /> is the children of <paramref name="group" /> or of any group inside it.
	/// </summary>
	private static bool IsSelfOrDescendant(
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
	/// Orders records by a key in a direction.
	/// </summary>
	private static IOrderedEnumerable<T> Order<T>(
		IEnumerable<T> records,
		Func<T, string?> key,
		ListSortDirection direction)
	{
		return direction switch
		{
			ListSortDirection.Ascending => records.OrderBy(key),
			ListSortDirection.Descending => records.OrderByDescending(key),
			_ => throw new NotImplementedException()
		};
	}

	/// <summary>
	/// Returns <paramref name="records" /> in the sorted order, groups first, then key and value records, then value records,
	/// and sorts the records of the groups in place.
	/// </summary>
	private static DatasetRecordBase[] SortRecords(
		ICollection<DatasetRecordBase> records,
		ListSortDirection direction)
	{
		RecordsGroup[] groups = [.. Order(records.OfType<RecordsGroup>(), static x => x.Name, direction)];

		KeyValueRecord[] keyValues = [.. Order(records.OfType<KeyValueRecord>(), static x => x.Key, direction)];

		ValueRecord[] values = [.. Order(records.OfType<ValueRecord>(), static x => x.Value, direction)];

		foreach (RecordsGroup group in groups)
		{
			DatasetRecordBase[] sorted = SortRecords(group.Children, direction);

			group
				.Children
				.ClearAddRange(sorted);
		}

		return [.. groups, .. keyValues, .. values];
	}
	#endregion
}
