using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Migrations;
using Repository.DbContexts;
using Repository.Interfaces;
using System.Data.Common;
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

	/// <summary>
	/// Returns <c>True</c> if the service was disposed.
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
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

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

	/// <inheritdoc />
	public Task<int> SaveChangesAsync(CancellationToken token = default) => _dbContext.SaveChangesAsync(token);
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
