using Entities.Models;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Repository for <see cref="ExplorerItemBase" />.
/// </summary>
public interface IExplorerItemRepository
{
	#region Methods
	/// <inheritdoc cref="RepositoryBase{T}.CountAsync" />
	Task<int> CountOfAsync(
		Expression<Func<ExplorerItemBase, bool>> condition,
		CancellationToken token = default);

	/// <inheritdoc cref="RepositoryBase{T}.ExistsAsync" />
	Task<bool> ExistsAsync(Expression<Func<ExplorerItemBase, bool>> condition, CancellationToken token = default);
	#endregion
}
