using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.Extensions;

public static class CollectionExtensions
{
	#region Methods
	/// <summary>
	/// Adds elements to the end of <see cref="ICollection{T}" />.
	/// </summary>
	public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			collection.Add(item);
		}
	}

	/// <summary>
	/// Clears, then adds elements to <see cref="ICollection{T}" />.
	/// </summary>
	public static void ClearAddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
	{
		collection.Clear();

		collection.AddRange(items);
	}

	/// <summary>
	/// Sorts the elements in <see cref="ICollection{T}" />.
	/// </summary>
	public static void SortBy<TSource, TKey>(this ICollection<TSource> collection, Func<TSource, TKey> keySelector)
	{
		if (collection.Count == 0)
		{
			return;
		}

		TSource[] ordered = [.. collection.OrderBy(keySelector)];

		collection.Clear();

		foreach (TSource item in ordered)
		{
			collection.Add(item);
		}
	}
	#endregion
}
