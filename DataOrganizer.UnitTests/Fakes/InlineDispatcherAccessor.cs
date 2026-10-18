using DataOrganizer.Enums.Views;
using DataOrganizer.Interfaces.Views;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.UnitTests.Fakes;

/// <summary>
/// Test-only <see cref="IDispatcherAccessor" /> that executes posted work synchronously on the
/// calling thread, making the effects of dispatcher-posted work observable in unit-test assertions.
/// </summary>
internal sealed class InlineDispatcherAccessor : IDispatcherAccessor
{
	#region Methods
	/// <inheritdoc />
	public void Post(Action action, UiPriority priority = default) => action();

	/// <inheritdoc />
	public Task PostAsync(Action action, UiPriority priority = default)
	{
		action();

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<TResult> func, UiPriority priority = default)
	{
		return Task.FromResult(func());
	}

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<Task<TResult>> func, UiPriority priority = default)
	{
		return func();
	}
	#endregion
}
