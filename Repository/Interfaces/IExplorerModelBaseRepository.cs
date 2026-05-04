using Entities.Abstract;
using Repository.Abstract;
using System;
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

	/// <inheritdoc cref="RepositoryBase{T}.IsExistsAsync" />
	Task<bool> IsExistsAsync(Expression<Func<ExplorerModelBase, bool>> condition, CancellationToken token = default);
	#endregion
}
