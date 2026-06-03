using Avalonia.Threading;
using DataOrganizer.Interfaces;
using System;
using System.Threading.Tasks;

namespace DataOrganizer.Services;

public sealed class DispatcherAccessor : IDispatcherAccessor
{
	#region Data
	/// <inheritdoc cref="IDispatcher" />
	private readonly IDispatcher _dispatcher;
	#endregion

	#region Constructors
	public DispatcherAccessor(IDispatcher dispatcher) => _dispatcher = dispatcher;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Post(Action action, DispatcherPriority priority = default) => _dispatcher.Post(action, priority);

	/// <inheritdoc />
	public Task PostAsync(Action action, DispatcherPriority priority = default)
	{
		TaskCompletionSource source = new();

		_dispatcher.Post(() =>
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

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<TResult> func, DispatcherPriority priority = default)
	{
		TaskCompletionSource<TResult> source = new();

		_dispatcher.Post(() =>
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

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<Task<TResult>> func, DispatcherPriority priority = default)
	{
		return PostAsync<Task<TResult>>(func, priority).Unwrap();
	}
	#endregion
}
