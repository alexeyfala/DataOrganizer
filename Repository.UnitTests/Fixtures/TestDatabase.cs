using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repository.DbContexts;
using System;

namespace Repository.UnitTests.Helpers;

/// <summary>
/// Owns an open in-memory SQLite connection and a schema-created <see cref="SqliteDbContext" /> for a single test.
/// </summary>
internal sealed class TestDatabase : IDisposable
{
	#region Data
	private readonly SqliteConnection _connection;
	#endregion

	#region Constructors
	public TestDatabase()
	{
		_connection = new SqliteConnection("DataSource=:memory:");

		_connection.Open();

		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseSqlite(_connection)
			.Options;

		Context = new SqliteDbContext(options);

		Context.Database.EnsureCreated();
	}
	#endregion

	#region Properties
	/// <summary>
	/// Context bound to the in-memory database.
	/// </summary>
	public SqliteDbContext Context { get; }
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		Context.Dispose();

		_connection.Dispose();
	}
	#endregion
}
