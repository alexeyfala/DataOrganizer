using Entities.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Repository.Abstract;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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
	Task AddRangeAsync(IEnumerable<FileModel> entities, CancellationToken token);

	/// <summary>
	/// Returns a complete flat list of <see cref="FileModel" /> entities from the database.
	/// </summary>
	Task<FileModel[]> GetAllAsync(
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default,
		params string[] excludedProperties);

	/// <summary>
	/// Makes a request <see cref="FileModel" /> from the database by identifier.
	/// </summary>
	Task<FileModel> GetAsync(
		Guid id,
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <summary>
	/// Returns a flat list of <see cref="FileModel" /> entities according to a condition from the database.
	/// </summary>
	Task<FileModel[]> GetAsync(
		Expression<Func<FileModel, bool>> condition,
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <summary>
	/// Returns a flat list of <see cref="FileModel" /> entities according to a list of IDs.
	/// </summary>
	Task<FileModel[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool includeDependencies = false
		, bool trackChanges = false,
		CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.Remove" />
	EntityEntry<FileModel> Remove(FileModel entity);

	/// <inheritdoc cref="RepositoryBase{T}.RemoveRange" />
	void RemoveRange(IEnumerable<FileModel> entities);
	#endregion Methods
}
