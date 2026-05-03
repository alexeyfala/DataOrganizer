using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
	public Task<FolderModel[]> GetAllAsync(CancellationToken token = default) => FindAll().ToArrayAsync(token);

	/// <inheritdoc />
	public async IAsyncEnumerable<Guid> GetFolderSubtreeIdsAsync(
		Guid rootId,
		[EnumeratorCancellation] CancellationToken token = default)
	{
		Queue<Guid> queue = new();

		queue.Enqueue(rootId);

		while (queue.TryDequeue(out Guid parentId))
		{
			yield return parentId;

			Guid[] childIds = await FindBy(x => x.ParentId == parentId)
				.Select(x => x.Id)
				.ToArrayAsync(token)
				.ConfigureAwait(false);

			foreach (Guid childId in childIds)
			{
				queue.Enqueue(childId);
			}
		}
	}

	/// <inheritdoc />
	public Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => ids.Contains(x.Id), token);
	}

	/// <inheritdoc />
	public Task<int> UpdatePropertiesAsync(
		Guid id,
		Action<UpdateSettersBuilder<FolderModel>>[] setters,
		CancellationToken token = default)
	{
		return ExecuteUpdateAsync(x => x.Id == id, setters, token);
	}
	#endregion Methods
}
