using Entities.Models;
using Repository.DbContexts;
using Repository.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

/// <inheritdoc cref="IExplorerItemRepository" />
public sealed class ExplorerItemRepository : RepositoryBase<ExplorerItemBase>, IExplorerItemRepository
{
	#region Constructors
	public ExplorerItemRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	[ExcludeFromCodeCoverage(Justification = "A thin wrapper over EF Core")]
	public Task<int> CountOfAsync(
		Expression<Func<ExplorerItemBase, bool>> condition,
		CancellationToken token = default)
	{
		return CountAsync(condition, token);
	}
	#endregion
}