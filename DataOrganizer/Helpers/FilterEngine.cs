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
/// Wraps a DynamicData <see cref="SourceList{T}"/> with a <c>Filter</c>/<c>Bind</c> pipeline and exposes
/// a read-only <see cref="Visible"/> projection of the source through a caller-supplied predicate stream.
/// Mutating methods (<see cref="InsertAndRebuild"/>, <see cref="Reorder"/>) trigger a full rebuild of the
/// source so that <see cref="Visible"/> reflects source order under <see cref="ListFilterPolicy.ClearAndReplace"/>.
/// Not thread-safe — intended to be created and used on the UI thread; the captured
/// <see cref="SynchronizationContext"/> is used to marshal change notifications back to that thread.
/// </summary>
internal sealed class FilterEngine<TModel> : IDisposable where TModel : INotifyPropertyChanged
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the underlying source contains no items at all.
	/// This reflects total contents — items hidden by an active filter still count.
	/// To check whether the currently shown subset is empty, use <c><see cref="Visible"/>.Count == 0</c>.
	/// </summary>
	public bool IsSourceEmpty => _source.Items.Count == 0;

	/// <summary>
	/// A visible sequence of <typeparamref name="TModel" />.
	/// </summary>
	public ReadOnlyObservableCollection<TModel> Visible => _visible;
	#endregion

	#region Data
	/// <inheritdoc cref="SynchronizationContext" />
	private readonly SynchronizationContext? _context;

	/// <inheritdoc cref="SourceList{T}" />
	private readonly SourceList<TModel> _source = new();

	/// <summary>
	/// Subscription handle for the bind pipeline; disposed in <see cref="Dispose"/>.
	/// </summary>
	private readonly IDisposable _subscription;

	/// <summary>
	/// A backing field for <see cref="Visible" />.
	/// </summary>
	private readonly ReadOnlyObservableCollection<TModel> _visible;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	/// <summary>
	/// Creates a new <see cref="FilterEngine{TModel}"/>.
	/// </summary>
	/// <param name="filterPredicate">Stream of predicates driving <see cref="Visible"/>.</param>
	/// <param name="autoRefreshOn">
	/// Optional property accessor that scopes <c>AutoRefresh</c> — the filter is re-evaluated only when
	/// this specific property changes on a source item (e.g. <c>x =&gt; x.Name</c>). Pass <c>null</c>
	/// to skip per-item INPC tracking entirely; in that case property changes on items already in the
	/// source will not trigger <see cref="Visible"/> recomputation until the predicate itself changes.
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
	/// Searches the entire source — items currently hidden by an active filter are still considered.
	/// </summary>
	public TModel? FirstOrDefaultFromSource(Func<TModel, bool> condition) => _source.Items.FirstOrDefault(condition);

	/// <summary>
	/// Inserts <paramref name="item"/> so that it lands at <paramref name="destinationVisibleIndex"/>
	/// in <see cref="Visible"/> after the source is rebuilt. Translation uses the item currently at that
	/// visible index as an anchor, so behavior is correct even when a filter is active.
	/// Does nothing if <paramref name="item"/> is already present in the source — to relocate an existing
	/// item use <see cref="Reorder"/>.
	/// Triggers a full rebuild of the source (and therefore <see cref="Visible"/>) — i.e. <c>ItemsControl</c>
	/// sees a Reset, so selection and scroll position are lost; restore them via <see cref="PostToUi"/> if needed.
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
	/// Posts <paramref name="action"/> to the captured <see cref="SynchronizationContext"/> so it runs
	/// after any pending <see cref="Visible"/> updates already queued on the UI thread (FIFO ordering of
	/// <see cref="SynchronizationContext.Post"/>). If no context was captured, runs synchronously.
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
	/// Moves <paramref name="item"/> within the source so that it lands at
	/// <paramref name="destinationVisibleIndex"/> in <see cref="Visible"/> after the rebuild.
	/// Translation uses the item currently at that visible index as an anchor, so behavior is
	/// correct even when a filter is active. Does nothing if <paramref name="item"/> is not in the source
	/// or if it is already at the resolved destination.
	/// Triggers a full rebuild of the source (and therefore <see cref="Visible"/>) — i.e. <c>ItemsControl</c>
	/// sees a Reset, so selection and scroll position are lost; restore them via <see cref="PostToUi"/> if needed.
	/// Performs two <c>O(N)</c> linear scans of the source (one to locate <paramref name="item"/>, one to
	/// resolve the anchor at <paramref name="destinationVisibleIndex"/>) plus the <c>O(N)</c> rebuild — not
	/// suited for collections in the tens of thousands of elements without further optimization.
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
	/// Projects every item in the source through <paramref name="selector"/>, in source order.
	/// Iterates the entire source — items currently hidden by an active filter are still included.
	/// </summary>
	public IEnumerable<TResult> SelectFromSource<TResult>(Func<TModel, TResult> selector) => _source.Items.Select(selector);
	#endregion

	#region Helpers
	/// <summary>
	/// Snapshots the source into a working list, applies <paramref name="mutate"/>, then replays the
	/// result via <c>Clear</c> + <c>AddRange</c> inside a single <see cref="ISourceList{T}.Edit"/>.
	/// The <c>Clear</c> step forces the <c>Filter</c> cache (under <see cref="ListFilterPolicy.ClearAndReplace"/>)
	/// to rebuild in the new source order, sidestepping the cache's insertion-order tracking.
	/// </summary>
	private void RebuildSourceWith(Action<List<TModel>> mutate) => _source.Edit(list =>
	{
		List<TModel> ordered = [.. list];

		mutate(ordered);

		list.Clear();

		list.AddRange(ordered);
	});

	/// <summary>
	/// Translates an index in <see cref="Visible"/> to the corresponding source index by looking up
	/// the item currently at that visible position. Past-end and negative inputs are clamped to the
	/// source ends. When no filter is active, this is a 1:1 mapping.
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
