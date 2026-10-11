using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Repository.DbContexts;
using Repository.Interceptors;
using Repository.Interfaces;
using Repository.Interfaces.Database;
using Repository.Services;
using Repository.Services.Database;
using System;
using System.Diagnostics;

namespace Repository.Extensions;

public static class RepositoryServiceCollectionExtensions
{
	#region Methods
	/// <summary>
	/// Registers the database layer over the SQLite file whose path <paramref name="getDatabaseFilePath" /> returns.
	/// </summary>
	public static IServiceCollection AddRepository(
		this IServiceCollection services,
		Func<IServiceProvider, string> getDatabaseFilePath)
	{
		services.AddDbContext<SqliteDbContext>((provider, builder) => ConfigureDbContext(
			builder,
			getDatabaseFilePath,
			provider));

		services.AddTransient<IDbMaintenance, DbMaintenance>();
		services.AddTransient<IExplorerItemRepository, ExplorerItemRepository>();
		services.AddTransient<IFileRepository, FileRepository>();
		services.AddTransient<IFolderRepository, FolderRepository>();
		services.AddTransient<IHotkeysRepository, HotkeysRepository>();

		services.AddSingleton<IDbAccess, DbAccess>();
		services.AddSingleton<IDbContextService, DbContextService>();

		return services;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Configures <see cref="SqliteDbContext" />.
	/// </summary>
	private static void ConfigureDbContext(
		DbContextOptionsBuilder builder,
		Func<IServiceProvider, string> getDatabaseFilePath,
		IServiceProvider provider)
	{
		try
		{
			SqliteConnectionStringBuilder connectionBuilder = new()
			{
				DataSource = getDatabaseFilePath(provider),
				Mode = SqliteOpenMode.ReadWriteCreate,
				RecursiveTriggers = false,
				DefaultTimeout = 30,
				Pooling = true
			};

			builder.UseSqlite(
				connectionBuilder.ToString(),
				x => x.MigrationsAssembly(SqliteDbContext.MigrationsAssemblyName));

			// Pooled connections are reused, and the pragmas do not survive in the database file.
			builder.AddInterceptors(new SqlitePragmaInterceptor());

			//ILogger logger = provider.GetRequiredService<ILogger>();

			//builder.LogTo(logger.LogDebugWithTemplate);
		}
		catch (Exception ex)
		{
			Trace.WriteLine(ex);
		}
	}
	#endregion
}
