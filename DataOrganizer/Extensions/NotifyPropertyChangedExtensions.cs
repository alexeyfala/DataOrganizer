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
		Expression<Func<T, string?>> whenValueChanged,
		string? startWith,
		Action emptyStringAction) where T : INotifyPropertyChanged
	{
		return target
			.WhenValueChanged(whenValueChanged, notifyOnInitialValue: false)
			.Throttle(TimeSpan.FromMilliseconds(500L))
			.StartWith(startWith) // Important for not to throttle when collection shown for the first time.
			.Select(Predicate);

		Func<IName, bool> Predicate(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				if (string.Equals(value, string.Empty))
				{
					emptyStringAction();
				}

				return _ => true;
			}

			return x => x.Name.Contains(value, StringComparison.OrdinalIgnoreCase);
		}
	}
	#endregion
}
