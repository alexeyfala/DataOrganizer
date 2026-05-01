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
	/// Returns a flat list of <see cref="FolderModel" /> entities according to a condition from the database.
	/// </summary>
	Task<FolderModel[]> GetAsync(
		Expression<Func<FolderModel, bool>> condition,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <summary>
	/// Returns a flat list of <see cref="FolderModel" /> entities according to a list of IDs.
	/// </summary>
	Task<FolderModel[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.Remove" />
	EntityEntry<FolderModel> Remove(FolderModel entity);

	/// <inheritdoc cref="RepositoryBase{T}.RemoveRange" />
	void RemoveRange(IEnumerable<FolderModel> entities);
	#endregion Methods
}
