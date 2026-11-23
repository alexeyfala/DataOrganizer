using CommunityToolkit.Mvvm.ComponentModel;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Helpers;
using DataOrganizer.Hierarchy.Interfaces;
using DataOrganizer.Shared.Threading.Extensions;
using DynamicData.Binding;
using System;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.UI.ViewModels.Hierarchy;

/// <summary>
/// View model of the search of the tree by the names of its objects.
/// </summary>
public sealed partial class HierarchySearchViewModel : ObservableDisposableBase
{
	#region Properties
	/// <inheritdoc cref="FilteredHierarchy.Changes" />
	public IObservable<HierarchyFilter> Changes => _filter.Changes;

	/// <inheritdoc cref="FilteredHierarchy.Current" />
	public HierarchyFilter Filter => _filter.Current;

	/// <summary>
	/// <c>True</c> when the applied search matches no object.
	/// </summary>
	[ObservableProperty]
	public partial bool IsNothingFound { get; private set; }

	/// <summary>
	/// Applied search; <c>null</c> while the tree shows every object.
	/// </summary>
	[ObservableProperty]
	public partial string? Query { get; private set; }

	/// <summary>
	/// Text typed into the search box.
	/// </summary>
	[ObservableProperty]
	public partial string? Text { get; set; }
	#endregion

	#region Data
	/// <inheritdoc cref="FilteredHierarchy" />
	private readonly FilteredHierarchy _filter;
	#endregion

	#region Constructors
	/// <summary>
	/// Filters the hierarchy by the search text after a pause in typing.
	/// </summary>
	public HierarchySearchViewModel(ISessionHierarchy sessionHierarchy, TimeProvider timeProvider)
	{
		_filter = new FilteredHierarchy(
			sessionHierarchy.Hierarchy,
			this
				.WhenValueChanged(x => x.Text)
				.WithSearchPause(timeProvider));

		_filter
			.Changes
			.Subscribe(Apply)
			.DisposeWith(_disposables);

		_filter.DisposeWith(_disposables);
	}
	#endregion

	#region Methods
	/// <summary>
	/// Empties the search box and shows every object at once.
	/// </summary>
	public void Clear()
	{
		Text = null;

		_filter.Clear();
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Shows the state of an applied filter.
	/// </summary>
	private void Apply(HierarchyFilter filter)
	{
		IsNothingFound = filter.IsNothingFound;

		Query = filter.Query;
	}
	#endregion
}
