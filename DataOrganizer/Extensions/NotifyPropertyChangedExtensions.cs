using DataOrganizer.Interfaces;
using DynamicData.Binding;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace DataOrganizer.Extensions;

internal static class NotifyPropertyChangedExtensions
{
	#region Methods
	/// <summary>
	/// Creates a predicate for filtering.
	/// </summary>
	public static IObservable<Func<IName, bool>> FilterPredicate<T>(
		this T target,
		Expression<Func<T, string?>> whenValueChanged) where T : INotifyPropertyChanged
	{
		return target
			.WhenValueChanged(whenValueChanged)
			.Publish(stream =>
			{
				return stream
					.Take(1)
					.Merge(stream.Skip(1).Throttle(TimeSpan.FromMilliseconds(500L)));
			}).Select(Predicate);

		static Func<IName, bool> Predicate(string? value) =>
			string.IsNullOrWhiteSpace(value)
				? _ => true
				: x => x.Name.Contains(value, StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}
