using Entities.Abstract;
using Repository.Abstract;
using System;
using System.Collections.Generic;
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
		CancellationToken token);

	/// <summary>
	/// Makes a request <see cref="ExplorerModelBase" /> from the database by identifier.
	/// </summary>
	Task<ExplorerModelBase> GetAsync(
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
	#endregion
}
