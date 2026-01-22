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
	public static void AddRange<T>(this ICollection<T> sequence, IEnumerable<T> items)
	{
		foreach (T item in items)
		{
			sequence.Add(item);
		}
	}

	/// <summary>
	/// Clears, then adds elements to <see cref="ICollection{T}" />.
	/// </summary>
	public static void ClearAddRange<T>(this ICollection<T> sequence, IEnumerable<T> items)
	{
		sequence.Clear();

		sequence.AddRange(items);
	}

	/// <summary>
	/// Sorts the elements in <see cref="ICollection{T}" />.
	/// </summary>
	public static void SortBy<TSource, TKey>(this ICollection<TSource> sequence, Func<TSource, TKey> keySelector)
	{
		if (sequence.Count == 0)
		{
			return;
		}

		TSource[] temp = sequence
			.OrderBy(keySelector)
			.ToArray();

		sequence.Clear();

		foreach (TSource item in temp)
		{
			sequence.Add(item);
		}
	}
	#endregion
}
