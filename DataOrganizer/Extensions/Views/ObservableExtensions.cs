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
		if (SynchronizationContext.Current is { } context)
		{
			return target
				.Throttle(delay)
				.ObserveOn(context);
		}

		return target;
	}
	#endregion
}
