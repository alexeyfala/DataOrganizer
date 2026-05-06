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
	Task<FolderModel[]> GetAllAsync(CancellationToken token = default);

	/// <summary>
	/// Returns IDs of the folder and all its nested folders (with BFS algorithm).
	/// </summary>
	IAsyncEnumerable<Guid> GetFolderSubtreeIdsAsync(Guid rootId, CancellationToken token = default);

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
		Action<UpdateSettersBuilder<FolderModel>>[] setters,
		CancellationToken token = default);
	#endregion Methods
}
