using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading;

namespace Threading.Helpers;

/// <summary>
/// Scheduler of Rx that counts its time by a <see cref="TimeProvider" />.
/// </summary>
public sealed class TimeProviderScheduler : IScheduler
{
	#region Properties
	/// <inheritdoc />
	public DateTimeOffset Now => _timeProvider.GetUtcNow();
	#endregion

	#region Data
	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public TimeProviderScheduler(TimeProvider timeProvider)
	{
		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
	{
		return Schedule(state, TimeSpan.Zero, action);
	}

	/// <inheritdoc />
	public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
	{
		return Schedule(state, dueTime - Now, action);
	}

	/// <inheritdoc />
	public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
	{
		SingleAssignmentDisposable work = new();

		ITimer timer = _timeProvider.CreateTimer(
			_ => work.Disposable = action(this, state),
			null,
			Scheduler.Normalize(dueTime),
			Timeout.InfiniteTimeSpan);

		return StableCompositeDisposable.Create(timer, work);
	}
	#endregion
}
