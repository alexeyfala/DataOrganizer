using Avalonia.Threading;
using DataOrganizer.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Helpers;

/// <summary>
/// Test-only <see cref="IDispatcherAccessor" /> that executes posted work synchronously on the
/// calling thread, making the effects of dispatcher-posted work observable in unit-test assertions.
/// </summary>
internal sealed class InlineDispatcherAccessor : IDispatcherAccessor
{
	#region Methods
	/// <inheritdoc />
	public void Post(Action action, DispatcherPriority priority = default) => action();


	/// <inheritdoc />
	public Task PostAsync(Action action, DispatcherPriority priority = default)
	{
		action();

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<TResult> func, DispatcherPriority priority = default)
	{
		return Task.FromResult(func());
	}

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<Task<TResult>> func, DispatcherPriority priority = default)
	{
		return func();
	}
	#endregion
}
