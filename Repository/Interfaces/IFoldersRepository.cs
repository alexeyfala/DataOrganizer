using Entities.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Repository.Abstract;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Repository for <see cref="FolderModel" />.
/// </summary>
public interface IFoldersRepository
{
	#region Methods
	/// <inheritdoc cref="RepositoryBase{T}.AddAsync" />
	ValueTask<EntityEntry<FolderModel>> AddAsync(FolderModel entity, CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.AddRangeAsync" />
	Task AddRangeAsync(IEnumerable<FolderModel> entities, CancellationToken token = default);

	/// <summary>
	/// Returns a complete flat list of <see cref="FolderModel" /> entities from the database.
	/// </summary>
	Task<FolderModel[]> GetAllAsync(bool trackChanges = false, CancellationToken token = default);

	/// <summary>
	/// Returns IDs of the folder and all its nested folders (with BFS algorithm).
	/// </summary>
	IAsyncEnumerable<Guid> GetFolderSubtreeIdsAsync(Guid rootId, CancellationToken token = default);

	/// <summary>
	/// Removes entities from the database by IDs.
	/// </summary>
	Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default);
	#endregion Methods
}
