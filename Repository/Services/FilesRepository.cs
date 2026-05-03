using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Interfaces;
using Shared.Extensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class FilesRepository : RepositoryBase<FileModel>, IFilesRepository
{
	#region Constructors
	public FilesRepository(SqliteDbContext context) : base(context)
	{
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public Task<FileModel?> FirstOrDefaultAsync(
		Guid id,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return FindBy(x => x.Id == id, trackChanges).FirstOrDefaultAsync(token);
	}

	/// <inheritdoc />
	public Task<FileModel[]> GetAllAsync(
		bool trackChanges = false,
		CancellationToken token = default,
		params string[] excludedProperties)
	{
		IQueryable<FileModel> query = excludedProperties.Length > 0
			? FindAll().Include(x => x.Hotkeys).Select(x => x.CopyPropertiesTo(excludedProperties))
			: FindAll(trackChanges);

		return query.ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<Guid[]> GetFileIdsAsync(Guid[] parentIds, CancellationToken token = default)
	{
		return FindBy(x => x.ParentId.HasValue && parentIds.Contains(x.ParentId.Value))
			.Select(x => x.Id)
			.ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<int> RemoveAsync(Guid id, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => x.Id == id, token);
	}

	/// <inheritdoc />
	public Task<int> RemoveRangeByIdsAsync(Guid[] ids, CancellationToken token = default)
	{
		return RemoveRangeByAsync(x => ids.Contains(x.Id), token);
	}
	#endregion Methods
}
