using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

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

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.AnyAsync{TSource}" />
	public Task<bool> IsExistsAsync(Expression<Func<T, bool>> condition, CancellationToken token)
	{
		return _context
			.Set<T>()
			.AsNoTracking()
			.AnyAsync(condition, token);
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

	/// <inheritdoc cref="EntityFrameworkQueryableExtensions.CountAsync{TSource}" />
	protected Task<int> CountAsync(Expression<Func<T, bool>> condition, CancellationToken token)
	{
		return _context
			.Set<T>()
			.AsNoTracking()
			.CountAsync(condition, token);
	}

	/// <summary>
	/// Updates the specified properties of entities matching <paramref name="condition" />.
	/// </summary>
	/// <param name="condition">Filter for affected rows.</param>
	/// <param name="setters">Property setters, e.g. <c>b =&gt; b.SetProperty(x =&gt; x.Foo, value)</c>.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>The number of rows affected.</returns>
	protected Task<int> ExecuteUpdateAsync(
		Expression<Func<T, bool>> condition,
		Action<UpdateSettersBuilder<T>>[] setters,
		CancellationToken token)
	{
		if (setters.Length == 0)
		{
			return Task.FromResult(0);
		}

		return _context
			.Set<T>()
			.Where(condition)
			.ExecuteUpdateAsync(builder =>
			{
				foreach (Action<UpdateSettersBuilder<T>> setter in setters)
				{
					setter(builder);
				}
			}, token);
	}

	/// <summary>
	/// Issues an <c>UPDATE</c> per <paramref name="updates" /> entry inside a single transaction.
	/// Each entry pairs a row filter with the setters to apply to those rows.
	/// </summary>
	/// <param name="updates">Pairs of (filter, setters). Entries with empty setter arrays are skipped.</param>
	/// <param name="token">Cancellation token.</param>
	/// <returns>The total number of rows affected across all updates.</returns>
	protected async Task<int> ExecuteUpdateRangeAsync(
		IEnumerable<KeyValuePair<Expression<Func<T, bool>>, Action<UpdateSettersBuilder<T>>[]>> updates,
		CancellationToken token)
	{
		await using IDbContextTransaction transaction = await _context
			.Database
			.BeginTransactionAsync(token)
			.ConfigureAwait(false);

		int total = 0;

		foreach (KeyValuePair<Expression<Func<T, bool>>, Action<UpdateSettersBuilder<T>>[]> update in updates)
		{
			total += await ExecuteUpdateAsync(
				update.Key,
				update.Value,
				token).ConfigureAwait(false);
		}

		await transaction
			.CommitAsync(token)
			.ConfigureAwait(false);

		return total;
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

	/// <summary>
	/// Removes entities from the database that meet the condition.
	/// </summary>
	protected Task<int> RemoveRangeByAsync(Expression<Func<T, bool>> condition, CancellationToken token)
	{
		return _context
			.Set<T>()
			.Where(condition)
			.ExecuteDeleteAsync(token);
	}
	#endregion
}
