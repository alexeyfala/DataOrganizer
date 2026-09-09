using DataOrganizer.Dto.Entities;
using Entities.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hierarchy;

/// <summary>
/// Provides methods for loading and mapping entities from a database.
/// </summary>
public interface IEntityLoader
{
	#region Methods
	/// <summary>
	/// Loads all entities from the database, maps them to the <see cref="ExplorerItemDtoBase" /> hierarchy, and returns the result.
	/// <c>Null</c> stands for a database that could not be read, which an empty hierarchy does not tell apart.
	/// </summary>
	Task<ExplorerItemDtoBase[]?> LoadFromEmbeddedDbAsync(CancellationToken token = default);

	/// <summary>
	/// Maps entities from the database to DTO objects.
	/// </summary>
	ExplorerItemDtoBase[] Map(IEnumerable<FolderEntity> dbFolders, IEnumerable<FileEntity> dbFiles);
	#endregion
}
