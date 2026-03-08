using Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Repository.DbContexts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Contains helper methods for <see cref="SqliteDbContext" />.
/// </summary>
public interface IDbContextService
{
	#region Methods
	/// <summary>
	/// Sets the <see cref="EntityEntry.State" /> property of entities to <see cref="EntityState.Detached" />.
	/// </summary>
	void Detach<T>(IEnumerable<T> entities) where T : class, IIdentity;

	/// <summary>
	/// Sets the <see cref="EntityEntry.State" /> property of an entity to <see cref="EntityState.Detached" />.
	/// </summary>
	void Detach<T>(Guid id) where T : class, IIdentity;

	/// <inheritdoc cref="DatabaseFacade.EnsureCreated" />
	void EnsureCreated();

	/// <inheritdoc cref="DatabaseFacade.EnsureCreatedAsync" />
	Task EnsureCreatedAsync(CancellationToken token = default);

	/// <inheritdoc cref="DatabaseFacade.EnsureDeleted" />
	void EnsureDeleted();

	/// <summary>
	/// Determines whether the assembly contains migration files.
	/// </summary>
	bool HasMigrations(Assembly assembly);

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.Migrate(DatabaseFacade)" />
	void Migrate();

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.MigrateAsync(DatabaseFacade, CancellationToken)" />
	Task MigrateAsync(CancellationToken token = default);
	#endregion
}
