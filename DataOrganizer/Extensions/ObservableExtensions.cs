using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;

namespace DataOrganizer.Extensions;

internal static class ObservableExtensions
{
	#region Methods
	/// <summary>
	/// Tries to set a delay.
	/// </summary>
	public static IObservable<EventPattern<TEventArgs>> SetDelay<TEventArgs>(
		this IObservable<EventPattern<TEventArgs>> target,
		in TimeSpan delay,
		in bool ignoreContext)
	{
		if (ignoreContext)
		{
			return target.Throttle(delay);
		}
		else if (SynchronizationContext.Current is { } context)
		{
			return target.Throttle(delay).ObserveOn(context);
		}

		return target;
	}
	#endregion
}
