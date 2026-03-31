using Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository.DbContexts;
using Repository.Interfaces;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class DbContextService : IDbContextService
{
	#region Data
	/// <inheritdoc cref="SqliteDbContext" />
	private readonly SqliteDbContext _dbContext;
	#endregion

	#region Constructors
	public DbContextService(SqliteDbContext dbContext) => _dbContext = dbContext;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Detach<T>(IEnumerable<T> entities) where T : class, IIdentity
	{
		LocalView<T> localView = GetLocalView<T>();

		if (localView.Count == 0)
		{
			return;
		}

		IEnumerable<Guid> identifiers = entities.Select(x => x.Id);

		localView
			.Where(x => identifiers.Contains(x.Id))
			.ForEach(x => SetEntryState(x, EntityState.Detached));
	}

	/// <inheritdoc />
	public void Detach<T>(Guid id) where T : class, IIdentity
	{
		LocalView<T> localView = GetLocalView<T>();

		if (localView.Count == 0 || localView.FirstOrDefault(x => x.Id == id) is not { } local)
		{
			return;
		}

		SetEntryState(local, EntityState.Detached);
	}

	/// <inheritdoc />
	public void EnsureCreated()
	{
		_dbContext
			.Database
			.EnsureCreated();
	}

	/// <inheritdoc />
	public Task EnsureCreatedAsync(CancellationToken token = default)
	{
		return _dbContext
			.Database
			.EnsureCreatedAsync(token);
	}

	/// <inheritdoc />
	public void EnsureDeleted()
	{
		_dbContext
			.Database
			.EnsureDeleted();
	}

	/// <inheritdoc />
	public bool HasMigrations(Assembly assembly)
	{
		return assembly
			.GetTypes()
			.Any(x => x.IsSubclassOf(typeof(Migration)));
	}

	/// <inheritdoc />
	public void Migrate()
	{
		_dbContext
			.Database
			.Migrate();
	}

	/// <inheritdoc />
	public Task MigrateAsync(CancellationToken token = default)
	{
		return _dbContext
			.Database
			.MigrateAsync(token);
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns <see cref="LocalView{T}" /> from <see cref="DbSet{T}" />.
	/// </summary>
	private LocalView<T> GetLocalView<T>() where T : class => _dbContext.Set<T>().Local;

	/// <summary>
	/// Sets the value of <see cref="EntityEntry.State" /> of an entity from <see cref="DbContext" />.
	/// </summary>
	private void SetEntryState<T>(T local, EntityState state) where T : class
	{
		EntityEntry<T> entry = _dbContext.Entry(local);

		entry.State = state;
	}
	#endregion
}
