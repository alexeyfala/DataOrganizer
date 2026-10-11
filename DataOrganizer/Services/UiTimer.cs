using DataOrganizer.Interfaces;
using System;
using System.Threading;

namespace DataOrganizer.Services;

public sealed class UiTimer : IUiTimer
{
	#region Data
	/// <inheritdoc cref="IDispatcherAccessor" />
	private readonly IDispatcherAccessor _dispatcher;

	/// <inheritdoc cref="TimeProvider" />
	private readonly TimeProvider _timeProvider;
	#endregion

	#region Constructors
	public UiTimer(IDispatcherAccessor dispatcher, TimeProvider timeProvider)
	{
		_dispatcher = dispatcher;

		_timeProvider = timeProvider;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Run(Func<bool> tick, TimeSpan interval)
	{
		ITimer? timer = null;

		// The next tick is scheduled only after the previous one has run, so ticks never pile up.
		void RunTick()
		{
			if (tick())
			{
				timer?.Change(interval, Timeout.InfiniteTimeSpan);

				return;
			}

			timer?.Dispose();
		}

		timer = _timeProvider.CreateTimer(
			_ => _dispatcher.Post(RunTick),
			null,
			interval,
			Timeout.InfiniteTimeSpan);
	}
	#endregion
}
