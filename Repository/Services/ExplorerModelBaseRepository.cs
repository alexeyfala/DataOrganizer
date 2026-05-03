using Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
	/// <inheritdoc />
	public Task<int> CountOfAsync(
		Expression<Func<ExplorerModelBase, bool>> condition,
		CancellationToken token = default)
	{
		return CountAsync(condition, token);
	}

	/// <inheritdoc />
	public Task<ExplorerModelBase?> FirstOrDefaultAsync(
		Guid id,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(x => x.Id == id, trackChanges).FirstOrDefaultAsync(token);
	}

	/// <inheritdoc />
	public Task<ExplorerModelBase[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(x => identifiers.Contains(x.Id), trackChanges).ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<int> UpdatePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<ExplorerModelBase>>[] setters,
		CancellationToken token = default)
	{
		return ExecuteUpdateAsync(x => x.Id == id, setters, token);
	}
	#endregion
}