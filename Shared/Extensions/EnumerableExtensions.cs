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
	/// Performs the specified action on each element of the sequence.
	/// </summary>
	public static Task ForEachAsync<T>(this IEnumerable<T> sequence, Func<T, Task> action)
	{
		return Task.WhenAll(sequence.Select(action));
	}

	/// <summary>
	/// Performs the specified action on each element of the sequence with bounded parallelism.
	/// </summary>
	/// <param name="maxDegreeOfParallelism">
	/// Maximum number of concurrently running tasks. Must be positive.
	/// Choose the value based on the bottleneck resource the action contends for, not on a generic default:
	/// <list type="bullet">
	/// <item><description>Database calls — match the connection pool size (or a fraction of it if shared with the rest of the app).</description></item>
	/// <item><description>HTTP / external APIs — respect the provider's rate limit and per-host connection limit (<see cref="System.Net.ServicePointManager.DefaultConnectionLimit"/> / <c>HttpClient</c> handler settings).</description></item>
	/// <item><description>File system / disk I/O — typically a small number (e.g. 4–16); higher values rarely help and can thrash on HDDs.</description></item>
	/// <item><description>CPU-bound work inside the action — use <see cref="Environment.ProcessorCount"/> as a starting point.</description></item>
	/// </list>
	/// Note: <see cref="Environment.ProcessorCount"/> is generally a poor default for I/O-bound work — it usually under-utilizes available throughput.
	/// If no throttling is required, use the overload without this parameter.
	/// </param>
	public static async Task ForEachAsync<T>(
		this IEnumerable<T> sequence,
		Func<T, Task> action,
		int maxDegreeOfParallelism)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism);

		using SemaphoreSlim semaphore = new(maxDegreeOfParallelism);

		Task[] tasks = [.. sequence.AsNotNull().Select(async item =>
		{
			await semaphore
				.WaitAsync()
				.ConfigureAwait(false);

			try
			{
				await action(item).ConfigureAwait(false);
			}
			finally
			{
				semaphore.Release();
			}
		})];

		await Task
			.WhenAll(tasks)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Performs the specified action on each element of the sequence with bounded parallelism, optimized for large collections.
	/// Spawns at most <paramref name="maxDegreeOfParallelism"/> worker tasks that pull items from the source enumerator,
	/// so memory and allocation pressure scale with the degree of parallelism, not with the size of the sequence.
	/// Fail-fast: on first exception, no new iterations start and <paramref name="action"/> receives a canceled token
	/// so in-flight iterations can abort early; the resulting task faults with the encountered exception(s).
	/// </summary>
	/// <param name="maxDegreeOfParallelism">Maximum number of concurrently running iterations. Must be positive.</param>
	/// <param name="cancellationToken">Token that cancels the overall operation and is forwarded to each invocation of <paramref name="action"/>.</param>
	public static Task ForEachAsync<T>(
		this IEnumerable<T> sequence,
		Func<T, CancellationToken, ValueTask> action,
		int maxDegreeOfParallelism,
		CancellationToken cancellationToken = default)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxDegreeOfParallelism);

		ParallelOptions options = new()
		{
			MaxDegreeOfParallelism = maxDegreeOfParallelism,
			CancellationToken = cancellationToken,
		};

		return Parallel.ForEachAsync(
			sequence.AsNotNull(),
			options,
			action);
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
