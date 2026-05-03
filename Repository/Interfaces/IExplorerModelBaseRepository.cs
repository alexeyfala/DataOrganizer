using Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Abstract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Repository for <see cref="ExplorerModelBase" />.
/// </summary>
public interface IExplorerModelBaseRepository
{
	#region Methods
	/// <inheritdoc cref="RepositoryBase{T}.CountAsync" />
	Task<int> CountOfAsync(
		Expression<Func<ExplorerModelBase, bool>> condition,
		CancellationToken token = default);

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.FirstOrDefaultAsync{TSource}(IQueryable{TSource}, CancellationToken)" />
	Task<ExplorerModelBase?> FirstOrDefaultAsync(
		Guid id,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <summary>
	/// Returns a flat list of <see cref="ExplorerModelBase" /> entities according to a list of IDs.
	/// </summary>
	Task<ExplorerModelBase[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool trackChanges = false,
		CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.IsExistsAsync" />
	Task<bool> IsExistsAsync(Expression<Func<ExplorerModelBase, bool>> condition, CancellationToken token = default);

	/// <summary>
	/// Updates the specified properties of the entity with the given <paramref name="id" />.
	/// </summary>
	/// <param name="id">Identifier of the entity to update.</param>
	/// <param name="setters">Property setters, e.g. <c>b =&gt; b.SetProperty(x =&gt; x.Name, "value")</c>.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>The number of rows affected (0 if the entity does not exist, otherwise 1).</returns>
	Task<int> UpdatePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<ExplorerModelBase>>[] setters,
		CancellationToken token = default);
	#endregion
}
