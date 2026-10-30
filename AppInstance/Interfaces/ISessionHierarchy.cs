using Hierarchy.Dto;
using System.Collections.ObjectModel;

namespace AppInstance.Interfaces;

/// <summary>
/// Tree of objects of the work session, shared by the main windows.
/// </summary>
public interface ISessionHierarchy
{
	#region Properties
	/// <summary>
	/// Hierarchical sequence of objects.
	/// </summary>
	ObservableCollection<ExplorerItemDtoBase> Hierarchy { get; }
	#endregion
}
