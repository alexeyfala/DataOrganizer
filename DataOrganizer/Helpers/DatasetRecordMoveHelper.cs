using DataOrganizer.DTO.Dataset;
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

		source.RemoveAt(oldIndex);

		// Removing from the same collection shifts every following slot left by one.
		if (ReferenceEquals(source, target) && targetIndex > oldIndex)
		{
			targetIndex--;
		}

		targetIndex = Math.Clamp(targetIndex, 0, target.Count);

		target.Insert(targetIndex, record);

		return true;
	}
	#endregion
}
