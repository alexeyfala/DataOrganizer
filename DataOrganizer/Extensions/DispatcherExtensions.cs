using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class DispatcherExtensions
{
	#region Methods
	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread asynchronously
	/// at the specified <paramref name="priority"/>.
	/// </summary>
	public static Task PostAsync(
		this IDispatcher target,
		Action action,
		DispatcherPriority priority = default)
	{
		TaskCompletionSource source = new();

		target.Post(() =>
		{
			try
			{
				action();

				source.SetResult();
			}
			catch (Exception ex)
			{
				source.SetException(ex);
			}
		}, priority);

		return source.Task;
	}

	/// <summary>
	/// Posts a function that will be invoked on the dispatcher thread asynchronously
	/// at the specified <paramref name="priority"/> and returns its result.
	/// </summary>
	public static Task<TResult> PostAsync<TResult>(
		this IDispatcher target,
		Func<TResult> func,
		DispatcherPriority priority = default)
	{
		TaskCompletionSource<TResult> source = new();

		target.Post(() =>
		{
			try
			{
				source.SetResult(func());
			}
			catch (Exception ex)
			{
				source.SetException(ex);
			}
		}, priority);

		return source.Task;
	}
	#endregion
}
