using DataOrganizer.Dto.Exchange;
using Hierarchy.Dto;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Exchange;

/// <summary>
/// Imports objects into the tree, with the open files closed first.
/// </summary>
public interface IImportScenario
{
	#region Methods
	/// <summary>
	/// Closes the open files after a question and imports the objects the user picks with their stored tree state; an
	/// import that replaces the hierarchy also empties it, the copy history and the tree state and forgets every key.
	/// <c>null</c> when nothing is imported.
	/// </summary>
	Task<ImportDataResult?> ImportAsync(Collection<ExplorerItemDtoBase> hierarchy);
	#endregion
}
