using Entities.Models;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Interfaces;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class ExplorerModelBaseRepository : RepositoryBase<ExplorerModelBase>, IExplorerModelBaseRepository
{
	#region Constructors
	public ExplorerModelBaseRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<int> CountOfAsync(
		Expression<Func<ExplorerModelBase, bool>> condition,
		CancellationToken token = default)
	{
		return CountAsync(condition, token);
	}
	#endregion
}