using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Runtime;
using System;
using System.Collections.ObjectModel;

namespace DataOrganizer.ViewModels.Hierarchy;

/// <summary>
/// View model of the row of the tree that shows an object.
/// </summary>
public abstract class ExplorerNodeViewModelBase : ObservableDisposableBase
{
	#region Data
	/// <inheritdoc cref="ISessionTreeState" />
	private readonly ISessionTreeState _treeState;
	#endregion

	#region Constructors
	protected ExplorerNodeViewModelBase(ExplorerItemDtoBase model, ISessionTreeState treeState)
	{
		Model = model;

		_treeState = treeState;
	}
	#endregion

	#region Properties
	/// <summary>
	/// Rows of the objects inside; empty for an object that holds none.
	/// </summary>
	public abstract ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Children { get; }

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
	#endregion

	#region Methods
	/// <summary>
	/// Creates the row of an object, with the rows of the objects inside a folder.
	/// </summary>
	public static ExplorerNodeViewModelBase Create(ExplorerItemDtoBase model, ISessionTreeState treeState)
	{
		return model switch
		{
			FolderDto folder => new FolderNodeViewModel(folder, treeState),
			FileDto file => new FileNodeViewModel(file, treeState),
			_ => throw new NotSupportedException($"The tree has no row for {model.GetType()}.")
		};
	}
	#endregion
}
