using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using DynamicData;
using DynamicData.Binding;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.ViewModels.Hierarchy;

/// <summary>
/// View model of the row of the tree that shows a folder, with the rows of its objects.
/// </summary>
public sealed class FolderNodeViewModel : ExplorerNodeViewModelBase
{
	#region Properties
	/// <inheritdoc />
	public override ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Children => _children;

	/// <inheritdoc />
	public override ReadOnlyObservableCollection<ExplorerNodeViewModelBase> ShownChildren => _shownChildren;
	#endregion

	#region Data
	/// <inheritdoc cref="Children" />
	private readonly ReadOnlyObservableCollection<ExplorerNodeViewModelBase> _children;

	/// <inheritdoc cref="ShownChildren" />
	private readonly ReadOnlyObservableCollection<ExplorerNodeViewModelBase> _shownChildren;
	#endregion

	#region Constructors
	public FolderNodeViewModel(
		FolderDto model,
		IObservable<HierarchyFilter> filter,
		ISessionTreeState treeState) : base(model, treeState)
	{
		// The filter comes after the disposal, so a row it hides stays alive until its object leaves the folder.
		model
			.Children
			.ToObservableChangeSet()
			.Transform(x => Create(
				x,
				filter,
				treeState))
			.DisposeMany()
			.Bind(out _children)
			.Filter(
				filter,
				static (state, row) => state.Shows(row.Model.Id))
			.Bind(out _shownChildren)
			.Subscribe()
			.DisposeWith(_disposables);
	}
	#endregion
}
