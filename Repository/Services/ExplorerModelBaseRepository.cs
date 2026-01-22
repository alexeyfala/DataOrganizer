using Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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
	/// <inheritdoc cref="RepositoryBase{T}.CountAsync(Expression{Func{T, bool}}, CancellationToken)" />
	public Task<int> CountOfAsync(
		Expression<Func<ExplorerModelBase, bool>> condition,
		CancellationToken token)
	{
		return CountAsync(condition, token);
	}

	/// <inheritdoc />
	public Task<ExplorerModelBase> GetAsync(
		Guid id,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(x => x.Id == id, trackChanges).FirstAsync(token);
	}

	/// <inheritdoc />
	public Task<ExplorerModelBase[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(x => identifiers.Contains(x.Id), trackChanges).ToArrayAsync(token);
	}
	#endregion
}