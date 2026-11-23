using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.FilteredLists.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace DataOrganizer.UI.ViewModels.Hierarchy;

/// <summary>
/// View model of the row of the tree that shows an object.
/// </summary>
public abstract class ExplorerNodeViewModelBase : ObservableDisposableBase
{
	#region Properties
	/// <summary>
	/// Rows of the objects inside; empty for an object that holds none.
	/// </summary>
	public abstract ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Children { get; }

	/// <inheritdoc cref="ISessionTreeState.IsChecked" />
	public bool? IsChecked => _treeState.IsChecked(Model.Id);

	/// <inheritdoc cref="ISessionTreeState.IsExpanded" />
	public bool IsExpanded
	{
		get => _treeState.IsExpanded(Model.Id);
		set => SetProperty(
			IsExpanded,
			value,
			this,
			static (node, isExpanded) => node._treeState.SetExpanded(node.Model.Id, isExpanded));
	}

	/// <inheritdoc cref="ISessionTreeState.IsSelected" />
	public bool IsSelected
	{
		get => _treeState.IsSelected(Model.Id);
		set => SetProperty(
			IsSelected,
			value,
			this,
			static (node, isSelected) => node._treeState.SetSelected(node.Model.Id, isSelected));
	}

	/// <summary>
	/// The object the row shows.
	/// </summary>
	public ExplorerItemDtoBase Model { get; }

	/// <summary>
	/// Rows of the objects inside that the name filter of the tree shows.
	/// </summary>
	public abstract ReadOnlyObservableCollection<ExplorerNodeViewModelBase> ShownChildren { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="ISessionTreeState" />
	private readonly ISessionTreeState _treeState;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="ExplorerNodeViewModelBase" /> class.
	/// </summary>
	protected ExplorerNodeViewModelBase(ExplorerItemDtoBase model, ISessionTreeState treeState)
	{
		Model = model;

		_treeState = treeState;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Creates the row of an object, with the rows of the objects inside a folder.
	/// </summary>
	public static ExplorerNodeViewModelBase Create(
		ExplorerItemDtoBase model,
		IObservable<HierarchyFilter> filter,
		ISessionTreeState treeState)
	{
		return model switch
		{
			FolderDto folder => new FolderNodeViewModel(
				folder,
				filter,
				treeState),
			FileDto file => new FileNodeViewModel(file, treeState)
		};
	}

	/// <summary>
	/// Raises the change of <see cref="IsChecked" /> after the work session changed the check mark of the object.
	/// </summary>
	public void NotifyIsCheckedChanged() => OnPropertyChanged(nameof(IsChecked));
	#endregion
}
