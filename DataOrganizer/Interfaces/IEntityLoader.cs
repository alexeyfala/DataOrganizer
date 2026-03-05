using DataOrganizer.DTO.Entities.Abstract;
using Entities.Models;
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
	/// Loads all <see cref="FolderModel" /> and all <see cref="FileModel" /> from database<br />
	/// then maps it to hierarchy of <see cref="ExplorerModelBaseDto" /> and returns.
	/// </summary>
	Task<ExplorerModelBaseDto[]> LoadAllHierarchyFromDbAsync(CancellationToken token = default);
	#endregion
}
