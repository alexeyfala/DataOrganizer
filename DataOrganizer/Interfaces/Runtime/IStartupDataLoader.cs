using Hierarchy.Dto;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Opens the database at launch and reads the tree of the work session from it.
/// </summary>
public interface IStartupDataLoader
{
	#region Methods
	/// <summary>
	/// Reads the tree with its stored state, empty when the database is unavailable or unreadable; <c>null</c> when the
	/// launch must end.
	/// </summary>
	Task<LoadedHierarchy?> LoadAsync(CancellationToken token = default);
	#endregion
}
