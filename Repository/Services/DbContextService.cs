using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository.DbContexts;
using Repository.Interfaces;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class DbContextService : IDbContextService
{
	#region Data
	/// <inheritdoc cref="SqliteDbContext" />
	private readonly SqliteDbContext _dbContext;

	/// <summary>
	/// <c>True</c> when the service has already been disposed.
	/// </summary>
	private bool _isDisposed;
	#endregion

	#region Constructors
	public DbContextService(SqliteDbContext dbContext) => _dbContext = dbContext;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, true))
		{
			return;
		}

		_dbContext.Dispose();
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
	public DbConnection GetDbConnection()
	{
		return _dbContext
			.Database
			.GetDbConnection();
	}

	/// <inheritdoc />
	public string GetDbFilePath()
	{
		return _dbContext
			.Database
			.GetDbConnection()
			.DataSource;
	}

	/// <inheritdoc />
	public bool HasMigrations()
	{
		return _dbContext
			.GetService<IMigrationsAssembly>()
			.Migrations
			.Count > 0;
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

	/// <inheritdoc />
	public Task<int> SaveChangesAsync(CancellationToken token = default) => _dbContext.SaveChangesAsync(token);
	#endregion
}
