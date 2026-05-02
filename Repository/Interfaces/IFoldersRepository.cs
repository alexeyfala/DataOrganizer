using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Repository.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
	Task AddRangeAsync(IEnumerable<FolderModel> entities, CancellationToken token);

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)" />
	Task<FolderModel?> FirstOrDefaultAsync(
		Guid id,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <summary>
	/// Returns a complete flat list of <see cref="FolderModel" /> entities from the database.
	/// </summary>
	Task<FolderModel[]> GetAllAsync(bool trackChanges = false, CancellationToken token = default);

	/// <summary>
	/// Returns IDs of the folder and all its nested folders (with BFS algorithm).
	/// </summary>
	IAsyncEnumerable<Guid> GetFolderSubtreeIdsAsync(Guid rootId, CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.Remove" />
	EntityEntry<FolderModel> Remove(FolderModel entity);

	/// <inheritdoc cref="RepositoryBase{T}.RemoveRange" />
	void RemoveRange(IEnumerable<FolderModel> entities);

	/// <summary>
	/// Removes entities from the database by IDs.
	/// </summary>
	Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default);
	#endregion Methods
}
