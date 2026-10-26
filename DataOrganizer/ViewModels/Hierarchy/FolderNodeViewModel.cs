using DataOrganizer.Interfaces.Runtime;
using DynamicData;
using DynamicData.Binding;
using Hierarchy.Dto;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;

namespace DataOrganizer.ViewModels.Hierarchy;

/// <summary>
/// View model of the row of the tree that shows a folder, with the rows of its objects.
/// </summary>
public sealed class FolderNodeViewModel : ExplorerNodeViewModelBase
{
	#region Data
	/// <inheritdoc cref="Children" />
	private readonly ReadOnlyObservableCollection<ExplorerNodeViewModelBase> _children;
	#endregion

	#region Constructors
	public FolderNodeViewModel(FolderDto model, ISessionTreeState treeState) : base(model, treeState)
	{
		model
			.Children
			.ToObservableChangeSet()
			.Transform(x => Create(x, treeState))
			.DisposeMany()
			.Bind(out _children)
			.Subscribe()
			.DisposeWith(_disposables);
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public override ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Children => _children;
	#endregion
}
