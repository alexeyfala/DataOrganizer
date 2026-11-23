using DataOrganizer.Hierarchy.Dto;
using DataOrganizer.Hierarchy.Interfaces;
using System.Collections.ObjectModel;

namespace DataOrganizer.UI.ViewModels.Hierarchy;

/// <summary>
/// View model of the row of the tree that shows a file or a dataset.
/// </summary>
public sealed class FileNodeViewModel : ExplorerNodeViewModelBase
{
	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// Always empty: a file holds no objects.
	/// </remarks>
	public override ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Children { get; } = new([]);

	/// <inheritdoc />
	/// <remarks>
	/// Always empty: a file holds no objects.
	/// </remarks>
	public override ReadOnlyObservableCollection<ExplorerNodeViewModelBase> ShownChildren => Children;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="FileNodeViewModel" /> class.
	/// </summary>
	public FileNodeViewModel(FileDto model, ISessionTreeState treeState) : base(model, treeState)
	{
	}
	#endregion
}
