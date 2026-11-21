using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace DataOrganizer.Hierarchy.FilteredLists.Helpers;

/// <summary>
/// Name filter of a tree of objects, driven by a stream of searches. Created and used on the UI thread: the searches reach
/// the tree through the <see cref="SynchronizationContext" /> of the thread that creates it.
/// </summary>
public sealed class FilteredHierarchy : IDisposable
{
	#region Properties
	/// <summary>
	/// The current filter, then each new one.
	/// </summary>
	public IObservable<HierarchyFilter> Changes => _filter;

	/// <summary>
	/// The filter applied now.
	/// </summary>
	public HierarchyFilter Current => _filter.Value;
	#endregion

	#region Data
	/// <summary>
	/// The current filter, which each new subscriber gets first.
	/// </summary>
	private readonly BehaviorSubject<HierarchyFilter> _filter = new(HierarchyFilter.None);

	/// <summary>
	/// Objects at the root of the tree.
	/// </summary>
	private readonly IEnumerable<ExplorerItemDtoBase> _hierarchy;

	/// <summary>
	/// Subscription to the searches.
	/// </summary>
	private readonly IDisposable _subscription;
	#endregion

	#region Constructors
	public FilteredHierarchy(
		IEnumerable<ExplorerItemDtoBase> hierarchy,
		IObservable<string?> queries)
	{
		_hierarchy = hierarchy;

		if (SynchronizationContext.Current is { } context)
		{
			// A search may come off the UI thread after its pause, while the tree is read on the UI thread only.
			queries = queries.ObserveOn(context);
		}

		_subscription = queries.Subscribe(Apply);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Shows every object at once.
	/// </summary>
	public void Clear() => Publish(HierarchyFilter.None);

	/// <inheritdoc />
	public void Dispose()
	{
		_subscription.Dispose();

		_filter.Dispose();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Collects an object when it or an object inside it matches, and its folder when an object inside is shown.
	/// </summary>
	private static bool Collect(
		ExplorerItemDtoBase item,
		Func<INamed, bool> matches,
		HashSet<Guid> shownIds,
		HashSet<Guid> expandedIds)
	{
		bool isAnyChildShown = false;

		if (item is FolderDto folder)
		{
			foreach (ExplorerItemDtoBase child in folder.Children)
			{
				isAnyChildShown |= Collect(
					child,
					matches,
					shownIds,
					expandedIds);
			}
		}

		if (isAnyChildShown)
		{
			expandedIds.Add(item.Id);
		}

		bool isShown = isAnyChildShown || matches(item);

		if (isShown)
		{
			shownIds.Add(item.Id);
		}

		return isShown;
	}

	/// <summary>
	/// Builds the filter of a search and publishes it.
	/// </summary>
	private void Apply(string? query)
	{
		if (NameFilter.IsBlank(query))
		{
			Publish(HierarchyFilter.None);

			return;
		}

		Func<INamed, bool> matches = NameFilter.Create(query);

		HashSet<Guid> shownIds = [];

		HashSet<Guid> expandedIds = [];

		foreach (ExplorerItemDtoBase item in _hierarchy)
		{
			_ = Collect(
				item,
				matches,
				shownIds,
				expandedIds);
		}

		Publish(new HierarchyFilter
		{
			ExpandedIds = expandedIds,
			Query = query,
			ShownIds = shownIds
		});
	}

	/// <summary>
	/// Publishes a filter, unless it and the current one both show every object.
	/// </summary>
	private void Publish(HierarchyFilter filter)
	{
		if (filter.Query is null && _filter.Value.Query is null)
		{
			return;
		}

		_filter.OnNext(filter);
	}
	#endregion
}
