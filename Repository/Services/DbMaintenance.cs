using Repository.Interceptors;
using Repository.Interfaces;
using Serilog;
using Shared.Extensions;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Services;

public sealed class DbMaintenance : IDbMaintenance
{
	#region Data
	/// <summary>
	/// Written to <c>PRAGMA user_version</c> once the free pages have been erased; it travels with the file.
	/// </summary>
	private const long SecureDeleteVersion = 1L;

	/// <inheritdoc cref="IDbContextService" />
	private readonly IDbContextService _dbContextService;

	/// <inheritdoc cref="ILogger" />
	private readonly ILogger _logger;
	#endregion

	#region Constructors
	public DbMaintenance(IDbContextService dbContextService, ILogger logger)
	{
		_dbContextService = dbContextService;

		_logger = logger;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public async Task EraseFreePagesOnceAsync(CancellationToken token = default)
	{
		DbConnection connection = _dbContextService.GetDbConnection();

		bool wasClosed = connection.State != ConnectionState.Open;

		if (wasClosed)
		{
			await connection
				.OpenAsync(token)
				.ConfigureAwait(false);

			// The interceptor sees only the connections the context opens itself.
			await SqlitePragmas
				.ApplyAsync(connection, token)
				.ConfigureAwait(false);
		}

		try
		{
			if (await ReadAsync(
				connection,
				"PRAGMA user_version;",
				token).ConfigureAwait(false) >= SecureDeleteVersion)
			{
				return;
			}

			long freePages = await ReadAsync(
				connection,
				"PRAGMA freelist_count;",
				token).ConfigureAwait(false);

			if (freePages > 0L)
			{
				_logger.LogInformation($"Rewriting the database to erase {freePages} free pages.");

				await ExecuteAsync(
					connection,
					"VACUUM;",
					token).ConfigureAwait(false);
			}

			await ExecuteAsync(
				connection,
				string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {SecureDeleteVersion};"),
				token).ConfigureAwait(false);
		}
		finally
		{
			if (wasClosed)
			{
				await connection
					.CloseAsync()
					.ConfigureAwait(false);
			}
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Executes a statement that returns nothing.
	/// </summary>
	private static async Task ExecuteAsync(DbConnection connection, string sql, CancellationToken token)
	{
		await using DbCommand command = connection.CreateCommand();

		command.CommandText = sql;

		await command
			.ExecuteNonQueryAsync(token)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Reads the single number a pragma reports.
	/// </summary>
	private static async Task<long> ReadAsync(DbConnection connection, string sql, CancellationToken token)
	{
		await using DbCommand command = connection.CreateCommand();

		command.CommandText = sql;

		object? value = await command
			.ExecuteScalarAsync(token)
			.ConfigureAwait(false);

		return value is long number ? number : 0L;
	}
	#endregion
}
