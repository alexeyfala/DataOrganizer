using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Repository.DbContexts;
using System;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interfaces;

/// <summary>
/// Contains helper methods for <see cref="SqliteDbContext" />.
/// </summary>
public interface IDbContextService : IDisposable
{
	#region Methods
	/// <inheritdoc cref="DatabaseFacade.EnsureCreated" />
	void EnsureCreated();

	/// <inheritdoc cref="DatabaseFacade.EnsureCreatedAsync" />
	Task EnsureCreatedAsync(CancellationToken token = default);

	/// <inheritdoc cref="DatabaseFacade.EnsureDeleted" />
	void EnsureDeleted();

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.GetDbConnection" />
	DbConnection GetDbConnection();

	/// <summary>
	/// Gets the database file path.
	/// </summary>
	string GetDbFilePath();

	/// <summary>
	/// Determines whether the assembly contains migration files.
	/// </summary>
	bool HasMigrations(Assembly assembly);

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.Migrate(DatabaseFacade)" />
	void Migrate();

	/// <inheritdoc cref="RelationalDatabaseFacadeExtensions.MigrateAsync(DatabaseFacade, CancellationToken)" />
	Task MigrateAsync(CancellationToken token = default);

	/// <inheritdoc cref="DbContext.SaveChangesAsync(CancellationToken)" />
	Task<int> SaveChangesAsync(CancellationToken token = default);
	#endregion
}
