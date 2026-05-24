using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
	/// Performs the specified action on each element of the sequence concurrently, without limiting parallelism.
	/// All tasks start at once; exceptions are aggregated into <see cref="AggregateException"/>.
	/// Suitable only for small or known-bounded sequences where resource saturation is not a concern.
	/// </summary>
	public static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> action)
	{
		return Task.WhenAll(sequence.Select(action));
	}

	/// <summary>
	/// Executes a synchronous <paramref name="action"/> over the sequence in parallel on the thread pool
	/// limited by <paramref name="maxDegreeOfParallelism"/>.
	/// </summary>
	/// <remarks>
	/// Use for CPU-bound or short, non-blocking work on bounded collections where parallelism must be capped
	/// (avoiding thread pool saturation from <see cref="Task.WhenAll(IEnumerable{Task})"/> with per-item <see cref="Task.Run(Action)"/>).
	/// Not intended for I/O-bound work — prefer the <see cref="Func{T, TResult}"/>-based overload.
	/// Exceptions thrown by <paramref name="action"/> are collected and rethrown as an <see cref="AggregateException"/>
	/// when the returned task is awaited.
	/// </remarks>
	public static Task ForEachAsync<T>(
		this IEnumerable<T> sequence,
		Action<T> action,
		int maxDegreeOfParallelism,
		CancellationToken token = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism);

		ParallelOptions options = new()
		{
			CancellationToken = token,
			MaxDegreeOfParallelism = maxDegreeOfParallelism,
		};

		return Parallel.ForEachAsync(sequence.AsNotNull(), options, (item, _) =>
		{
			action(item);

			return ValueTask.CompletedTask;
		});
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
	/// <returns>An ordered sequence in which elements from the original are present and were not found in the other.</returns>
	public static IEnumerable<T> OrderBySequenceKeepSource<T, TOrdered>(
		this IEnumerable<T> source,
		IEnumerable<TOrdered> ordered,
		Func<T, TOrdered> selector)
	{
		ILookup<TOrdered, T> lookup = source.ToLookup(selector, x => x);

		HashSet<TOrdered> included = [];

		foreach (TOrdered orderedItem in ordered)
		{
			if (!lookup.Contains(orderedItem))
			{
				continue;
			}

			foreach (T lookupItem in lookup[orderedItem])
			{
				yield return lookupItem;

				included.Add(orderedItem);
			}
		}

		foreach (T item in source)
		{
			if (!included.Contains(selector(item)))
			{
				yield return item;
			}
		}
	}

	/// <summary>
	/// Converts a sequence to a delimited string.
	/// </summary>
	public static string SplitAsString<T>(
		this IEnumerable<T> sequence,
		string separator,
		bool addSeparatorToEnd = false)
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		foreach (T item in sequence.AsNotNull())
		{
			if (builder.Length != 0)
			{
				builder.Append(separator);
			}

			builder.Append(item);
		}

		if (addSeparatorToEnd && builder.Length > 0)
		{
			builder.Append(separator);
		}

		return builder.ToString();
	}
	#endregion
}
