using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Extensions;

public static class EnumerableExtensions
{
	#region Methods
	/// <summary>
	/// Checks a sequence for null, if the condition is true, returns an empty sequence.
	/// </summary>
	public static IEnumerable<T> AsNotNull<T>(this IEnumerable<T>? sequence) => sequence ?? [];

	/// <summary>
	/// Checks an array for null, if the condition is true, returns an empty array.
	/// </summary>
	public static T[] AsNotNull<T>(this T[]? sequence) => sequence ?? [];

	/// <summary>
	/// Performs the specified action on each element of the sequence.
	/// </summary>
	public static IEnumerable<T> ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
	{
		foreach (T item in sequence.AsNotNull())
		{
			action(item);
		}

		return sequence;
	}

	/// <summary>
	/// Performs the specified action on each element of the sequence.
	/// </summary>
	public static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> action)
	{
		return Task.WhenAll(sequence.Select(action));
	}

	/// <summary>
	/// Performs the specified action on each element of a sequence, providing both the element and its index.
	/// </summary>
	public static void ForEachFor<T>(this IEnumerable<T> sequence, Action<T, int> action)
	{
		T[] array = [.. sequence.AsNotNull()];

		for (int i = 0; i < array.Length; i++)
		{
			action(array[i], i);
		}
	}

	/// <summary>
	/// Filters a sequence by a specific type, ignoring the base type.
	/// </summary>
	public static IEnumerable<TResult> OfSpecificType<TSource, TResult>(this IEnumerable<TSource> sequence) where TSource : class
	{
		return sequence
			.Where(x => x.GetType() == typeof(TResult))
			.OfType<TResult>();
	}

	/// <summary>
	/// Orders the source sequence by the properties of another.
	/// </summary>
	/// <typeparam name="T">Type of the original sequence.</typeparam>
	/// <typeparam name="TOrdered">Type of ordered sequence.</typeparam>
	/// <param name="source">Original sequence.</param>
	/// <param name="ordered">Ordered sequence.</param>
	/// <param name="selector">Property - selector.</param>
	/// <returns>An ordered sequence in which elements from the original are missing and were not found in the ordered sequence.</returns>
	public static IEnumerable<T> OrderBySequence<T, TOrdered>(
		this IEnumerable<T> source,
		IEnumerable<TOrdered> ordered,
		Func<T, TOrdered> selector)
	{
		ILookup<TOrdered, T> lookup = source.ToLookup(selector, x => x);

		foreach (TOrdered orderedItem in ordered)
		{
			foreach (T lookupItem in lookup[orderedItem])
			{
				yield return lookupItem;
			}
		}
	}
	#endregion
}
