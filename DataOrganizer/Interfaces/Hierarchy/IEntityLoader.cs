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
	/// Loads all entities from the database and maps them to the <see cref="ExplorerItemDtoBase" /> hierarchy with its stored
	/// tree state. <c>Null</c> stands for a database that could not be read, which an empty hierarchy does not tell apart.
	/// </summary>
	Task<LoadedHierarchy?> LoadHierarchyAsync(CancellationToken token = default);

	/// <summary>
	/// Maps entities from the database to DTO objects, with the ones stored expanded and selected.
	/// </summary>
	LoadedHierarchy Map(IReadOnlyCollection<FolderEntity> folders, IReadOnlyCollection<FileEntity> files);
	#endregion
}
