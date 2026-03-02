using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Extensions;

internal static class DspatcherExtensions
{
	#region Methods
	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread asynchronously.
	/// </summary>
	public static Task PostAsync(this IDispatcher target, Action action)
	{
		TaskCompletionSource source = new();

		target.Post(() =>
		{
			action();

			source.SetResult();
		});

		return source.Task;
	}
	#endregion
}
