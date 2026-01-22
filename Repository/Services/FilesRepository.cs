using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Abstract;
using Repository.DbContexts;
using Repository.Extensions;
using Repository.Interfaces;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
	public Task<FileModel[]> GetAllAsync(
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default,
		params string[] excludedProperties)
	{
		IQueryable<FileModel> query = excludedProperties.Length > 0
			? FindAll().Select(x => x.CopyPropertiesTo(excludedProperties))
			: FindAll(trackChanges);

		return includeDependencies
			? query.IncludeDependencies().ToArrayAsync(token)
			: query.ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<FileModel> GetAsync(
		Guid id,
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return includeDependencies
			? FindBy(x => x.Id == id, trackChanges).IncludeDependencies().FirstAsync(token)
			: FindBy(x => x.Id == id, trackChanges).FirstAsync(token);
	}

	/// <inheritdoc />
	public Task<FileModel[]> GetAsync(
		Expression<Func<FileModel, bool>> condition,
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return includeDependencies
			? FindBy(condition, trackChanges).IncludeDependencies().ToArrayAsync(token)
			: FindBy(condition, trackChanges).ToArrayAsync(token);
	}

	/// <inheritdoc />
	public Task<FileModel[]> GetAsync(
		IEnumerable<Guid> identifiers,
		bool includeDependencies = false,
		bool trackChanges = false,
		CancellationToken token = default)
	{
		return includeDependencies
			? FindBy(x => identifiers.Contains(x.Id), trackChanges).IncludeDependencies().ToArrayAsync(token)
			: FindBy(x => identifiers.Contains(x.Id), trackChanges).ToArrayAsync(token);
	}
	#endregion Methods
}
