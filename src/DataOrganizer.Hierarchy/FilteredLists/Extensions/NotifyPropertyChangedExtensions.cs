using DataOrganizer.Hierarchy.FilteredLists.Helpers;
using DataOrganizer.Hierarchy.Interfaces;
using DataOrganizer.Shared.Threading.Extensions;
using DynamicData.Binding;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace DataOrganizer.Hierarchy.FilteredLists.Extensions;

/// <summary>
/// Turns a search property of an object into a predicate that filters by name.
/// </summary>
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
			.WithSearchPause(timeProvider)
			.Select(NameFilter.Create);
	}
	#endregion
}
