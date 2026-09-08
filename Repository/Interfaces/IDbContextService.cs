using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Repository.DbContexts;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Contains helper methods for <see cref="SqliteDbContext" />.
/// </summary>
public interface IDbContextService : IDisposable
{
	#region Methods
	/// <inheritdoc cref="DatabaseFacade.CanConnectAsync(CancellationToken)" />
	Task<bool> CanConnectAsync(CancellationToken token = default);

	/// <inheritdoc cref="DatabaseFacade.EnsureCreated" />
	void EnsureCreated();

	/// <inheritdoc cref="DatabaseFacade.EnsureCreatedAsync" />
	Task EnsureCreatedAsync(CancellationToken token = default);

	/// <inheritdoc cref="DatabaseFacade.EnsureDeleted" />
	void EnsureDeleted();

	/// <summary>
	/// Runs the action inside a single database transaction and commits it; a failure rolls back
	/// everything the action has written.
	/// </summary>
	Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken token = default);

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.GetAppliedMigrationsAsync(DatabaseFacade, CancellationToken)" />
	Task<IEnumerable<string>> GetAppliedMigrationsAsync(CancellationToken token = default);

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.GetDbConnection" />
	DbConnection GetDbConnection();

	/// <summary>
	/// Gets the database file path.
	/// </summary>
	string GetDbFilePath();

	/// <summary>
	/// Identifiers of every migration the context's migrations assembly contains.
	/// </summary>
	IEnumerable<string> GetKnownMigrations();

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.GetPendingMigrationsAsync(DatabaseFacade, CancellationToken)" />
	Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken token = default);

	/// <summary>
	/// Determines whether the context's configured migrations assembly contains any migrations.
	/// </summary>
	bool HasMigrations();

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.Migrate(DatabaseFacade)" />
	void Migrate();

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.MigrateAsync(DatabaseFacade, CancellationToken)" />
	Task MigrateAsync(CancellationToken token = default);

	/// <inheritdoc cref="DbContext.SaveChangesAsync(CancellationToken)" />
	Task<int> SaveChangesAsync(CancellationToken token = default);
	#endregion
}
