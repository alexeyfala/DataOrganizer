using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading;

namespace DataOrganizer.Helpers;

// Sources:
// https://docs.avaloniaui.net/docs/concepts/reactiveui/binding-to-sorted-filtered-list
// https://www.reactiveui.net/docs/handbook/collections
// https://github.com/reactivemarbles/DynamicData
// https://habr.com/ru/articles/454074
// Nuget: DynamicData

/// <summary>
/// Wraps a DynamicData <see cref="SourceList{T}"/> with a Filter/Bind pipeline, exposing a read-only
/// projection driven by a caller-supplied predicate stream. Not thread-safe — created and used on the
/// UI thread, with the captured <see cref="SynchronizationContext"/> marshalling notifications back to it.
/// </summary>
internal sealed class FilterEngine<TModel> : IDisposable where TModel : INotifyPropertyChanged
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the source holds no items at all, including items hidden by an active filter.
	/// </summary>
	public bool IsSourceEmpty => _source.Items.Count == 0;

	/// <summary>
	/// Read-only projection of the source filtered by the current predicate.
	/// </summary>
	public ReadOnlyObservableCollection<TModel> Visible => _visible;
	#endregion

	#region Data
	/// <inheritdoc cref="SynchronizationContext" />
	private readonly SynchronizationContext? _context;

	/// <inheritdoc cref="SourceList{T}" />
	private readonly SourceList<TModel> _source = new();

	/// <summary>
	/// Subscription handle for the bind pipeline.
	/// </summary>
	private readonly IDisposable _subscription;

	/// <summary>
	/// Backing collection bound by the pipeline.
	/// </summary>
	private readonly ReadOnlyObservableCollection<TModel> _visible;

	/// <summary>
	/// <c>True</c> once disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	/// <summary>
	/// Creates a new <see cref="FilterEngine{TModel}"/>.
	/// </summary>
	/// <param name="filterPredicate">Stream of predicates that drive the filter.</param>
	/// <param name="autoRefreshOn">
	/// Optional property accessor that scopes <c>AutoRefresh</c> — the filter is re-evaluated only when this
	/// property changes on a source item (e.g. <c>x =&gt; x.Name</c>). Pass <c>null</c> to skip per-item INPC
	/// tracking; property changes on existing items then have no effect until the predicate itself changes.
	/// </param>
	public FilterEngine(
		IObservable<Func<TModel, bool>> filterPredicate,
		Expression<Func<TModel, object?>>? autoRefreshOn = null)
	{
		_context = SynchronizationContext.Current;

		IObservable<IChangeSet<TModel>> observable = _source.Connect();

		if (autoRefreshOn is not null)
		{
			observable = observable.AutoRefresh(autoRefreshOn);
		}

		observable = observable.Filter(filterPredicate, ListFilterPolicy.ClearAndReplace);

		if (_context is not null)
		{
			observable = observable.ObserveOn(_context);
		}

		_subscription = observable
			.Bind(out _visible)
			.Subscribe();
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="SourceListEditConvenienceEx.AddRange{T}(ISourceList{T}, IEnumerable{T})" />
	public void AddRange(IEnumerable<TModel> items) => _source.AddRange(items);

	/// <inheritdoc cref="SourceListEditConvenienceEx.Clear{T}(ISourceList{T})" />
	public void Clear() => _source.Clear();

	/// <summary>
	/// <c>True</c> when <paramref name="item"/> is present in the source.
	/// </summary>
	public bool Contains(TModel item) => _source.Items.Contains(item);

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		_source.Clear();

		_subscription.Dispose();

		_source.Dispose();
	}

	/// <summary>
	/// Returns the first source item matching <paramref name="condition"/>, or <c>null</c> if none match.
	/// Searches the whole source, including items hidden by an active filter.
	/// </summary>
	public TModel? FirstOrDefaultFromSource(Func<TModel, bool> condition) => _source.Items.FirstOrDefault(condition);

	/// <summary>
	/// Inserts <paramref name="item"/> to land at <paramref name="destinationVisibleIndex"/> in the visible
	/// list after a rebuild, anchoring on the item at that index so it works under a filter. No-op if already
	/// present; triggers a full source rebuild (a Reset), so selection and scroll position are lost.
	/// </summary>
	public void InsertAndRebuild(TModel item, int destinationVisibleIndex)
	{
		if (Contains(item))
		{
			return;
		}

		int sourceDestination = TranslateVisibleToSource(destinationVisibleIndex);

		RebuildSourceWith(ordered => ordered.Insert(Math.Min(sourceDestination, ordered.Count), item));
	}

	/// <summary>
	/// Posts <paramref name="action"/> to the captured <see cref="SynchronizationContext"/> so it runs after
	/// any pending updates already queued on the UI thread. Runs synchronously if no context was captured.
	/// </summary>
	public void PostToUi(Action action)
	{
		if (_context is { } context)
		{
			context.Post(_ => action(), null);
		}
		else
		{
			action();
		}
	}

	/// <inheritdoc cref="SourceListEditConvenienceEx.Remove" />
	public bool Remove(TModel item) => _source.Remove(item);

	/// <summary>
	/// Moves <paramref name="item"/> to land at <paramref name="destinationVisibleIndex"/> in the visible list,
	/// anchoring on the item at that index so it works under a filter. No-op if absent or already at the target.
	/// Rebuilds the source (a Reset), so selection and scroll are lost; <c>O(N)</c>, unsuited to huge collections.
	/// </summary>
	public void Reorder(TModel item, int destinationVisibleIndex)
	{
		int sourceOriginal = _source
			.Items
			.IndexOf(item);

		if (sourceOriginal < 0)
		{
			return;
		}

		int sourceDestination = TranslateVisibleToSource(destinationVisibleIndex);

		if (sourceOriginal == sourceDestination)
		{
			return;
		}

		RebuildSourceWith(ordered =>
		{
			ordered.RemoveAt(sourceOriginal);

			ordered.Insert(Math.Min(sourceDestination, ordered.Count), item);
		});
	}

	/// <summary>
	/// Projects every source item through <paramref name="selector"/> in source order, including items
	/// hidden by an active filter.
	/// </summary>
	public IEnumerable<TResult> SelectFromSource<TResult>(Func<TModel, TResult> selector) => _source.Items.Select(selector);
	#endregion

	#region Helpers
	/// <summary>
	/// Snapshots the source, applies <paramref name="mutate"/>, then replays the result via Clear + AddRange
	/// in one edit so the filter cache rebuilds in the new source order.
	/// </summary>
	private void RebuildSourceWith(Action<List<TModel>> mutate) => _source.Edit(list =>
	{
		List<TModel> ordered = [.. list];

		mutate(ordered);

		list.Clear();

		list.AddRange(ordered);
	});

	/// <summary>
	/// Translates an index in the visible list to the matching source index via the item at that position.
	/// Out-of-range inputs are clamped to the source ends; a 1:1 mapping when no filter is active.
	/// </summary>
	private int TranslateVisibleToSource(int destinationVisibleIndex)
	{
		if (destinationVisibleIndex >= _visible.Count)
		{
			return _source
				.Items
				.Count;
		}

		if (destinationVisibleIndex <= 0)
		{
			return 0;
		}

		return _source
			.Items
			.IndexOf(_visible[destinationVisibleIndex]);
	}
	#endregion
}
