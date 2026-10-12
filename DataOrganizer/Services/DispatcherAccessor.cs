using Avalonia.Threading;
using DataOrganizer.Enums;
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
	public void Post(Action action, UiPriority priority = default) => _dispatcher.Post(action, ToDispatcherPriority(priority));

	/// <inheritdoc />
	public Task PostAsync(Action action, UiPriority priority = default)
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
		}, ToDispatcherPriority(priority));

		return source.Task;
	}

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<TResult> func, UiPriority priority = default)
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
		}, ToDispatcherPriority(priority));

		return source.Task;
	}

	/// <inheritdoc />
	public Task<TResult> PostAsync<TResult>(Func<Task<TResult>> func, UiPriority priority = default)
	{
		return PostAsync<Task<TResult>>(func, priority).Unwrap();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the Avalonia priority of <paramref name="priority" />.
	/// </summary>
	private static DispatcherPriority ToDispatcherPriority(UiPriority priority) => priority switch
	{
		UiPriority.Default => default,
		UiPriority.Background => DispatcherPriority.Background,
		UiPriority.Loaded => DispatcherPriority.Loaded,
		_ => throw new NotImplementedException()
	};
	#endregion
}
