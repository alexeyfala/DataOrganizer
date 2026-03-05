using DataOrganizer.DTO.Entities.Abstract;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods for loading and mapping entities from a database.
/// </summary>
public interface IEntityLoader
{
	#region Methods
	/// <summary>
	/// Loads all entities from the specified database, maps them to the <see cref="ExplorerModelBaseDto" /> hierarchy, and returns the result.
	/// </summary>
	ExplorerModelBaseDto[] LoadFromDb(string dataSource);

	/// <summary>
	/// Loads all entities from the database, maps them to the <see cref="ExplorerModelBaseDto" /> hierarchy, and returns the result.
	/// </summary>
	Task<ExplorerModelBaseDto[]> LoadFromEmbeddedDbAsync(CancellationToken token = default);
	#endregion
}
