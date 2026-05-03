using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Repository.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Repository for <see cref="FileModel" />.
/// </summary>
public interface IFilesRepository
{
	#region Methods
	/// <inheritdoc cref="RepositoryBase{T}.AddAsync" />
	ValueTask<EntityEntry<FileModel>> AddAsync(FileModel entity, CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.AddRangeAsync" />
	Task AddRangeAsync(IEnumerable<FileModel> entities, CancellationToken token = default);

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)" />
	Task<FileModel?> FirstOrDefaultAsync(
		Guid id,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <summary>
	/// Returns a complete flat list of <see cref="FileModel" /> entities from the database.
	/// </summary>
	Task<FileModel[]> GetAllAsync(CancellationToken token = default);

	/// <summary>
	/// Returns file IDs by parent IDs.
	/// </summary>
	Task<Guid[]> GetFileIdsAsync(Guid[] parentIds, CancellationToken token = default);

	/// <summary>
	/// Removes entity from the database by Id.
	/// </summary>
	Task<int> RemoveAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Removes entities from the database by IDs.
	/// </summary>
	Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default);
	#endregion Methods
}
