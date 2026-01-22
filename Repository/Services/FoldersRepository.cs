using Entities.Models;
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

public sealed class FoldersRepository : RepositoryBase<FolderModel>, IFoldersRepository
{
	#region Constructors
	public FoldersRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<FolderModel[]> GetAllAsync(bool trackChanges = false, CancellationToken token = default)
	{
		return FindAll(trackChanges).ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<FolderModel> GetAsync(Guid id, bool trackChanges = false, CancellationToken token = default)
	{
		return FindBy(x => x.Id == id, trackChanges).FirstAsync(token);
	}

	/// <inheritdoc />
	public Task<FolderModel[]> GetAsync(
		Expression<Func<FolderModel, bool>> condition,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(condition, trackChanges).ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<FolderModel[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(x => identifiers.Contains(x.Id), trackChanges).ToArrayAsync(token);
	}
	#endregion Methods
}
