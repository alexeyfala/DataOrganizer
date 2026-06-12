using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Repository.DbContexts;

namespace Repository.Migrations;

/// <summary>
/// Design-time factory used by EF Core tools (dotnet ef) to create
/// <see cref="SqliteDbContext" /> when adding or applying migrations.
/// </summary>
public sealed class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
	#region Methods
	/// <inheritdoc />
	public SqliteDbContext CreateDbContext(string[] args)
	{
		// The connection string is irrelevant for scaffolding migrations; it only has to be a
		// valid SQLite source. MigrationsAssembly points EF Core at this project.
		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseSqlite("Data Source=design-time.db", x => x.MigrationsAssembly(SqliteDbContext.MigrationsAssemblyName))
			.Options;

		return new SqliteDbContext(options);
	}
	#endregion
}
