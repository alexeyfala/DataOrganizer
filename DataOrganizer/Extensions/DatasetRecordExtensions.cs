using DataOrganizer.Models.Dataset;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DataOrganizer.Extensions;

internal static class DatasetRecordExtensions
{
	#region Methods
	/// <summary>
	/// Performs a transformation of a hierarchical sequence into a flat one.
	/// </summary>
	public static IEnumerable<DatasetRecordBase> Flatten(this IEnumerable<DatasetRecordBase> hierarchy)
	{
		Stack<DatasetRecordBase> stack = new(hierarchy);

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
	/// Counts objects in the hierarchy.
	/// </summary>
	public static int GetCount(this IEnumerable<DatasetRecordBase> hierarchy)
	{
		return hierarchy
			.Flatten()
			.Count();
	}

	/// <summary>
	/// Sorts <paramref name="records"/> in a required order.
	/// </summary>
	public static DatasetRecordBase[] Sort(
		this ICollection<DatasetRecordBase> records,
		ListSortDirection direction)
	{
		RecordsGroup[] groups = direction switch
		{
			ListSortDirection.Ascending => [.. records.OfType<RecordsGroup>().OrderBy(x => x.Name)],
			ListSortDirection.Descending => [.. records.OfType<RecordsGroup>().OrderByDescending(x => x.Name)],
			_ => throw new NotImplementedException()
		};

		KeyValueRecord[] keyValues = direction switch
		{
			ListSortDirection.Ascending => [.. records.OfSpecificType<DatasetRecordBase, KeyValueRecord>().OrderBy(x => x.Key)],
			ListSortDirection.Descending => [.. records.OfSpecificType<DatasetRecordBase, KeyValueRecord>().OrderByDescending(x => x.Key)],
			_ => throw new NotImplementedException()
		};

		ValueRecord[] values = direction switch
		{
			ListSortDirection.Ascending => [.. records.OfSpecificType<DatasetRecordBase, ValueRecord>().OrderBy(x => x.Value)],
			ListSortDirection.Descending => [.. records.OfSpecificType<DatasetRecordBase, ValueRecord>().OrderByDescending(x => x.Value)],
			_ => throw new NotImplementedException()
		};

		foreach (RecordsGroup group in groups)
		{
			DatasetRecordBase[] sorted = group
				.Children
				.Sort(direction);

			group
				.Children
				.ClearAddRange(sorted);
		}

		return [.. groups, .. keyValues, .. values];
	}
	#endregion
}
