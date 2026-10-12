using DataOrganizer.Enums;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides means to interact with Avalonia's UI dispatcher.
/// </summary>
public interface IDispatcherAccessor
{
	#region Methods
	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread at the specified <paramref name="priority"/>.
	/// </summary>
	void Post(Action action, UiPriority priority = default);

	/// <summary>
	/// Posts an action that will be invoked on the dispatcher thread asynchronously
	/// at the specified <paramref name="priority"/>.
	/// </summary>
	Task PostAsync(Action action, UiPriority priority = default);

	/// <summary>
	/// Posts a function that will be invoked on the dispatcher thread asynchronously
	/// at the specified <paramref name="priority"/> and returns its result.
	/// </summary>
	Task<TResult> PostAsync<TResult>(Func<TResult> func, UiPriority priority = default);

	/// <summary>
	/// Posts an asynchronous <paramref name="func"/> that is started on the dispatcher thread
	/// at the specified <paramref name="priority"/> and returns its unwrapped result.
	/// </summary>
	Task<TResult> PostAsync<TResult>(Func<Task<TResult>> func, UiPriority priority = default);
	#endregion
}
