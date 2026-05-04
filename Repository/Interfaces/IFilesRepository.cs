using Entities.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Abstract;
using System;
using System.Collections.Generic;
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

	/// <summary>
	/// Returns a complete flat list of <see cref="FileModel" /> entities from the database.
	/// </summary>
	Task<FileModel[]> GetAllAsync(CancellationToken token = default);

	/// <summary>
	/// Returns value from <see cref="FileModel.Contents" />.
	/// </summary>
	Task<byte[]?> GetContentsAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Returns file IDs by parent IDs.
	/// </summary>
	Task<Guid[]> GetFileIdsAsync(Guid[] parentIds, CancellationToken token = default);

	/// <summary>
	/// Returns value from <see cref="FileModel.Properties" />.
	/// </summary>
	Task<string?> GetPropertiesAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Removes entity from the database by Id.
	/// </summary>
	Task<int> RemoveAsync(Guid id, CancellationToken token = default);

	/// <summary>
	/// Removes entities from the database by IDs.
	/// </summary>
	Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default);

	/// <summary>
	/// Updates the specified properties of the entity with the given <paramref name="id" />.
	/// </summary>
	/// <param name="id">Identifier of the entity to update.</param>
	/// <param name="setters">Property setters, e.g. <c>b =&gt; b.SetProperty(x =&gt; x.Name, "value")</c>.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>The number of rows affected (0 if the entity does not exist, otherwise 1).</returns>
	Task<int> UpdatePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FileModel>>[] setters,
		CancellationToken token = default);

	/// <summary>
	/// Updates properties of multiple entities in a single transaction. Each dictionary entry
	/// maps an entity Id to the setters to apply to that entity.
	/// </summary>
	/// <param name="updates">Map from entity Id to its property setters.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>The total number of rows affected across all updates.</returns>
	Task<int> UpdatePropertiesAsync(
		IDictionary<Guid, Action<UpdateSettersBuilder<FileModel>>[]> updates,
		CancellationToken token = default);
	#endregion Methods
}
