using Hierarchy.Dto;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hierarchy;

/// <summary>
/// Deletes an object of the tree together with what depends on it.
/// </summary>
public interface IDeletionScenario
{
	#region Methods
	/// <summary>
	/// Deletes an object from the database and from the hierarchy, closes its files that are open, takes them out of the
	/// copy history and forgets the keys of a deleted folder; <c>false</c> when nothing is deleted.
	/// </summary>
	Task<bool> DeleteAsync(
		ExplorerItemDtoBase item,
		Collection<ExplorerItemDtoBase> hierarchy,
		CancellationToken token = default);
	#endregion
}
