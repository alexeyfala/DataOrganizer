using DataOrganizer.Database.Repository.DbContexts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace DataOrganizer.TestSupport.Database;

/// <summary>
/// Owns an open in-memory SQLite connection and a schema-created <see cref="SqliteDbContext" /> for a single test.
/// </summary>
public sealed class TestDatabase : IDisposable
{
	#region Properties
	/// <summary>
	/// Context bound to the in-memory database.
	/// </summary>
	public SqliteDbContext Context { get; }
	#endregion

	#region Data
	private readonly SqliteConnection _connection;
	#endregion

	#region Constructors
	/// <summary>
	/// Opens an in-memory SQLite database and creates its schema.
	/// </summary>
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

	#region Methods
	/// <inheritdoc />
	public void Dispose()
	{
		Context.Dispose();

		_connection.Dispose();
	}
	#endregion
}
