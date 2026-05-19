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
			action();

			source.SetResult();
		}, priority);

		return source.Task;
	}
	#endregion
}
