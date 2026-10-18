using DataOrganizer.Dto.Entities;
using DataOrganizer.Interfaces.Runtime;
using System.Collections.ObjectModel;

namespace DataOrganizer.ViewModels.Hierarchy;

/// <summary>
/// View model of the row of the tree that shows a file or a dataset.
/// </summary>
public sealed class FileNodeViewModel : ExplorerNodeViewModelBase
{
	#region Constructors
	public FileNodeViewModel(FileDto model, ISessionTreeState treeState) : base(model, treeState)
	{
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	/// <remarks>
	/// Always empty: a file holds no objects.
	/// </remarks>
	public override ReadOnlyObservableCollection<ExplorerNodeViewModelBase> Children { get; } = new([]);
	#endregion
}
