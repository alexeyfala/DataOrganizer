using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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
/// Filtering engine for <typeparamref name="TModel" />.
/// </summary>
internal sealed class FilterEngine<TModel> : IDisposable where TModel : INotifyPropertyChanged
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if the source is empty.
	/// </summary>
	public bool IsEmpty => _source.Items.Count == 0;

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
	/// A backing field for <see cref="Visible" />.
	/// </summary>
	private readonly ReadOnlyObservableCollection<TModel> _visible;
	#endregion

	#region Constructors
	public FilterEngine(
		IObservable<Func<TModel, bool>> filterPredicate,
		SortExpressionComparer<TModel> sortComparer)
	{
		_context = SynchronizationContext.Current;

		IObservable<IChangeSet<TModel>> observable = _source
			.Connect()
			.AutoRefresh()
			.Filter(filterPredicate);

		if (_context is not null)
		{
			observable = observable.ObserveOn(_context);
		}

		observable
			.Sort(sortComparer)
			.Bind(out _visible)
			.Subscribe();
	}
	#endregion

	#region Methods
	/// <inheritdoc cref="SourceListEditConvenienceEx.AddRange{T}(ISourceList{T}, IEnumerable{T})" />
	public void AddRange(IEnumerable<TModel> items) => _source.AddRange(items);

	/// <inheritdoc cref="SourceListEditConvenienceEx.Clear{T}(ISourceList{T})" />
	public void Clear() => _source.Clear();

	/// <inheritdoc />
	public void Dispose()
	{
		_source.Clear();

		_source.Dispose();
	}

	/// <inheritdoc cref="Enumerable.ElementAt{TSource}(IEnumerable{TSource}, int)" />
	public TModel ElementAt(in int index) => _source.Items.ElementAt(index);

	/// <inheritdoc cref="Enumerable.FirstOrDefault{TSource}(IEnumerable{TSource}, Func{TSource, bool})" />
	public TModel? FirstOrDefault(Func<TModel, bool> condition) => _source.Items.FirstOrDefault(condition);

	/// <summary>
	/// Iterates the source and passes to <paramref name="action"/> item and index.
	/// </summary>
	public void IterateSource(Action<TModel, int> action)
	{
		for (int i = 0; i < _source.Items.Count; i++)
		{
			action(_source.Items[i], i);
		}
	}

	/// <inheritdoc cref="SourceListEditConvenienceEx.Move{T}(ISourceList{T}, int, int)" />
	public void Move(in int original, int destination) => _source.Move(original, destination);

	/// <inheritdoc cref="Enumerable.Select{TSource, TResult}(IEnumerable{TSource}, Func{TSource, TResult})" />
	public IEnumerable<TResult> Select<TResult>(Func<TModel, TResult> selector) => _source.Items.Select(selector);

	/// <summary>
	/// Executes the delegate from <paramref name="action"/>, if possible, in <see cref="SynchronizationContext.Post(SendOrPostCallback, object?)" />.
	/// </summary>
	public void Synchronize(Action action)
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
	#endregion
}
