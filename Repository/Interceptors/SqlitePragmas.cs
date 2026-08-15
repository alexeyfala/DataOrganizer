using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Repository.Interceptors;

/// <summary>
/// Connection-level SQLite pragmas that keep freed pages and temporary data off the disk.
/// </summary>
public static class SqlitePragmas
{
	#region Data
	/// <summary>
	/// The pragmas applied to every connection; SQLite does not store them in the database file.
	/// </summary>
	private const string Statements = "PRAGMA secure_delete = ON; PRAGMA temp_store = MEMORY;";
	#endregion

	#region Methods
	/// <summary>
	/// Applies the pragmas to an already opened connection.
	/// </summary>
	public static void Apply(DbConnection connection)
	{
		using DbCommand command = connection.CreateCommand();

		command.CommandText = Statements;

		command.ExecuteNonQuery();
	}

	/// <inheritdoc cref="Apply" />
	public static async Task ApplyAsync(DbConnection connection, CancellationToken token = default)
	{
		await using DbCommand command = connection.CreateCommand();

		command.CommandText = Statements;

		await command
			.ExecuteNonQueryAsync(token)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Opens the connection and applies the pragmas to it.
	/// </summary>
	public static void Open(SqliteConnection connection)
	{
		connection.Open();

		Apply(connection);
	}
	#endregion
}
