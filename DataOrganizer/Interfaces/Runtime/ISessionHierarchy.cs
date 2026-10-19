using DataOrganizer.Dto.Entities;
using System.Collections.ObjectModel;

namespace DataOrganizer.Interfaces.Runtime;

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
