using DataOrganizer.Helpers.Threading;
using System;
using System.Reactive.Linq;
using System.Threading;

namespace DataOrganizer.Extensions.Views;

internal static class ObservableExtensions
{
	#region Methods
	/// <summary>
	/// Tries to set a delay.
	/// </summary>
	public static IObservable<T> SetDelay<T>(
		this IObservable<T> target,
		TimeSpan delay)
	{
		return target.SetDelay(delay, TimeProvider.System);
	}

	/// <summary>
	/// Tries to set a delay counted by <paramref name="timeProvider" />.
	/// </summary>
	public static IObservable<T> SetDelay<T>(
		this IObservable<T> target,
		TimeSpan delay,
		TimeProvider timeProvider)
	{
		if (SynchronizationContext.Current is { } context)
		{
			return target
				.Throttle(delay, new TimeProviderScheduler(timeProvider))
				.ObserveOn(context);
		}

		return target;
	}
	#endregion
}
