using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods to interact with Avalonia's UI dispatcher.
/// </summary>
public interface IDispatcherAccessor
{
	#region Methods
	/// <inheritdoc cref="IDispatcher.Post(Action, DispatcherPriority)" />
	void Post(Action action, DispatcherPriority priority = default);

	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread asynchronously
	/// at the specified <paramref name="priority"/>.
	/// </summary>
	Task PostAsync(Action action, DispatcherPriority priority = default);

	/// <summary>
	/// Posts a function that will be invoked on the dispatcher thread asynchronously
	/// at the specified <paramref name="priority"/> and returns its result.
	/// </summary>
	Task<TResult> PostAsync<TResult>(Func<TResult> func, DispatcherPriority priority = default);

	/// <summary>
	/// Posts an asynchronous <paramref name="func"/> that is started on the dispatcher thread
	/// at the specified <paramref name="priority"/> and returns its unwrapped result.
	/// </summary>
	Task<TResult> PostAsync<TResult>(Func<Task<TResult>> func, DispatcherPriority priority = default);
	#endregion
}
