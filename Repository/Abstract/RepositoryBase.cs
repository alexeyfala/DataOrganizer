using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Abstract;

/// <summary>
/// Base repository for entities.
/// </summary>
public abstract class RepositoryBase<T> where T : class
{
	#region Data
	/// <inheritdoc cref="DbContext" />
	private readonly DbContext _context;
	#endregion

	#region Constructors
	protected RepositoryBase(DbContext context) => _context = context;
	#endregion

	#region Methods
	/// <inheritdoc cref="DbSet{T}.AddAsync" />
	public ValueTask<EntityEntry<T>> AddAsync(T entity, CancellationToken token)
	{
		return _context
			.Set<T>()
			.AddAsync(entity, token);
	}

	/// <inheritdoc cref="DbSet{T}.AddRangeAsync" />
	public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken token)
	{
		return _context
			.Set<T>()
			.AddRangeAsync(entities, token);
	}

	/// <inheritdoc cref="DbSet{T}.Remove(T)" />
	public EntityEntry<T> Remove(T entity)
	{
		return _context
			.Set<T>()
			.Remove(entity);
	}

	/// <inheritdoc cref="DbSet{T}.RemoveRange" />
	public void RemoveRange(IEnumerable<T> entities)
	{
		_context
			.Set<T>()
			.RemoveRange(entities);
	}

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}" />
	protected Task<bool> IsExistsAsync(Expression<Func<T, bool>> condition, CancellationToken token)
	{
		return _context
			.Set<T>()
			.AsNoTracking()
			.AnyAsync(condition, token);
	}

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}" />
	protected Task<int> CountAsync(Expression<Func<T, bool>> condition, CancellationToken token)
	{
		return _context
			.Set<T>()
			.AsNoTracking()
			.CountAsync(condition, token);
	}

	/// <summary>
	/// Searches for all <see cref="T" /> entities in the database.
	/// </summary>
	protected IQueryable<T> FindAll(bool trackChanges = false)
	{
		return trackChanges
			? _context.Set<T>()
			: _context.Set<T>().AsNoTracking();
	}

	/// <summary>
	/// Searches for an entity <see cref="T" /> in the database according to a specific condition.
	/// </summary>
	protected IQueryable<T> FindBy(Expression<Func<T, bool>> condition, bool trackChanges = false)
	{
		return trackChanges
			? _context.Set<T>().Where(condition)
			: _context.Set<T>().Where(condition).AsNoTracking();
	}
	#endregion
}
