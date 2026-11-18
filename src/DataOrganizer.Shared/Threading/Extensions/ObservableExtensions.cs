using DataOrganizer.Shared.Threading.Helpers;
using System;
using System.Reactive.Linq;

namespace DataOrganizer.Shared.Threading.Extensions;

public static class ObservableExtensions
{
	#region Data
	/// <summary>
	/// Pause in typing after which a changed search is applied.
	/// </summary>
	private static readonly TimeSpan SearchPause = TimeSpan.FromMilliseconds(500L);
	#endregion

	#region Methods
	/// <summary>
	/// Passes the first value of a search at once and each later one after a pause in typing counted by
	/// <paramref name="timeProvider" />.
	/// </summary>
	public static IObservable<T> WithSearchPause<T>(
		this IObservable<T> source,
		TimeProvider timeProvider)
	{
		return source.Publish(stream => stream
			.Take(1)
			.Merge(stream
				.Skip(1)
				.Throttle(
					SearchPause,
					new TimeProviderScheduler(timeProvider))));
	}
	#endregion
}
