using DynamicData.Binding;
using Hierarchy.Interfaces;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;
using Threading.Helpers;

namespace FilteredLists.Extensions;

public static class NotifyPropertyChangedExtensions
{
	#region Methods
	/// <summary>
	/// Creates a predicate for filtering, which follows a changed search after a pause counted by
	/// <paramref name="timeProvider" />.
	/// </summary>
	public static IObservable<Func<INamed, bool>> FilterPredicate<T>(
		this T target,
		Expression<Func<T, string?>> whenValueChanged,
		TimeProvider timeProvider) where T : INotifyPropertyChanged
	{
		return target
			.WhenValueChanged(whenValueChanged)
			.Publish(stream =>
			{
				return stream
					.Take(1)
					.Merge(stream.Skip(1).Throttle(
						TimeSpan.FromMilliseconds(500L),
						new TimeProviderScheduler(timeProvider)));
			}).Select(Predicate);

		static Func<INamed, bool> Predicate(string? value) =>
			string.IsNullOrWhiteSpace(value)
				? _ => true
				: x => x.Name.Contains(value, StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}
