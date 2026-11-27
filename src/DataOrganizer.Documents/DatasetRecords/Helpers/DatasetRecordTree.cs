using DataOrganizer.Documents.DatasetRecords.Enums;
using DataOrganizer.Documents.DatasetRecords.Models;
using DataOrganizer.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.Documents.DatasetRecords.Helpers;

/// <summary>
/// Rules of the record tree of a dataset: adding, deleting, moving and sorting records and finding the collection that
/// holds a record.
/// </summary>
public static class DatasetRecordTree
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

			// Stryker disable once Equality : an empty group has nothing to delete, so the check only saves the call
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
		return records.Flatten(GetChildren);
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
	/// key and value records, then value records; <c>false</c>, with nothing changed, when the sort would lose records.
	/// </summary>
	public static bool Sort(
		ObservableCollection<DatasetRecordBase> records,
		ListSortDirection direction)
	{
		// The records of a group come before the collection that holds it, so each list lays out its groups at their
		// final size.
		return ApplyOrders(
		[
			.. Flatten(records)
				.OfType<RecordsGroup>()
				.Reverse()
				.Select(x => (x.Children, OrderRecords(x.Children, direction))),
			(records, OrderRecords(records, direction))
		]);
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

				index = group
					.Children
					.Count;

				return true;

			// Dropping onto a record inserts before or after it, depending on the pointer half.
			case DatasetRecordBase record:
				if (ReferenceEquals(record, dragged)
					|| FindOwner(records, record) is not { } owner
					|| (dragged is RecordsGroup group2 && IsSelfOrDescendant(group2, owner)))
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

	/// <summary>
	/// Gives each collection of <paramref name="plan" /> its new order, in the order of the plan; <c>false</c>, with nothing
	/// changed, when any order loses or adds records.
	/// </summary>
	internal static bool ApplyOrders(
		(ObservableCollection<DatasetRecordBase> Records, DatasetRecordBase[] Order)[] plan)
	{
		// Every collection gets its new order before any of them changes, so a faulty order never reaches the records.
		if (!plan.All(static x => HoldsSameRecords(x.Records, x.Order)))
		{
			return false;
		}

		foreach ((ObservableCollection<DatasetRecordBase> target, DatasetRecordBase[] order) in plan)
		{
			target.ClearAddRange(order);
		}

		return true;
	}

	/// <summary>
	/// <c>True</c> when <paramref name="order" /> is <paramref name="records" /> rearranged: as many records, none lost and
	/// none added.
	/// </summary>
	internal static bool HoldsSameRecords(
		IReadOnlyCollection<DatasetRecordBase> records,
		IReadOnlyCollection<DatasetRecordBase> order)
	{
		HashSet<DatasetRecordBase> ordered = [with(ReferenceEqualityComparer.Instance), .. order];

		return order.Count == records.Count && ordered.SetEquals(records);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the records of <paramref name="record" />: those of a group, none of a single record.
	/// </summary>
	private static IEnumerable<DatasetRecordBase> GetChildren(DatasetRecordBase record)
	{
		if (record is RecordsGroup group)
		{
			return group.Children;
		}

		return [];
	}

	/// <summary>
	/// The rank of the kind and the text by which a record is sorted: groups by name, then key and value records by key,
	/// then value records by value.
	/// </summary>
	private static (int Rank, string? Text) GetSortKey(DatasetRecordBase record) => record switch
	{
		RecordsGroup group => (0, group.Name),
		KeyValueRecord keyValue => (1, keyValue.Key),
		ValueRecord value => (2, value.Value)
	};

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
	/// Returns <paramref name="records" /> in the sorted order: groups first, then key and value records, then value records,
	/// each kind by its text in <paramref name="direction" />.
	/// </summary>
	private static DatasetRecordBase[] OrderRecords(
		IEnumerable<DatasetRecordBase> records,
		ListSortDirection direction)
	{
		IOrderedEnumerable<DatasetRecordBase> byKind = records.OrderBy(static x => GetSortKey(x).Rank);

		return direction switch
		{
			ListSortDirection.Ascending => [.. byKind.ThenBy(static x => GetSortKey(x).Text)],
			ListSortDirection.Descending => [.. byKind.ThenByDescending(static x => GetSortKey(x).Text)],
			_ => throw new NotImplementedException()
		};
	}
	#endregion
}
