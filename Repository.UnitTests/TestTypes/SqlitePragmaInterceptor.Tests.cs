using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Repository.DbContexts;
using Repository.Interceptors;
using System.Data.Common;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SqlitePragmaInterceptor)}"" type")]
internal class SqlitePragmaInterceptorTests
{
	#region Data
	/// <summary>
	/// The value <c>PRAGMA temp_store</c> reports for <c>MEMORY</c>.
	/// </summary>
	private const long TempStoreMemory = 2L;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SqlitePragmaInterceptor.ConnectionOpened" />: the pragmas are in place once the context opens a connection.
	/// </summary>
	[Test]
	public void ConnectionOpened_Sets_The_Pragmas()
	{
		// Arrange
		using SqliteDbContext context = CreateContext();

		// Act
		context
			.Database
			.OpenConnection();

		// Assert
		DbConnection connection = context
			.Database
			.GetDbConnection();

		ReadPragma(connection, "secure_delete")
			.Should()
			.Be(1L);

		ReadPragma(connection, "temp_store")
			.Should()
			.Be(TempStoreMemory);
	}

	/// <summary>
	/// <see cref="SqlitePragmaInterceptor.ConnectionOpenedAsync" />: the pragmas are in place once the context opens a connection.
	/// </summary>
	[Test]
	public async Task ConnectionOpenedAsync_Sets_The_Pragmas()
	{
		// Arrange
		await using SqliteDbContext context = CreateContext();

		// Act
		await context
			.Database
			.OpenConnectionAsync();

		// Assert
		DbConnection connection = context
			.Database
			.GetDbConnection();

		ReadPragma(connection, "secure_delete")
			.Should()
			.Be(1L);

		ReadPragma(connection, "temp_store")
			.Should()
			.Be(TempStoreMemory);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a context over a database that lives only for the duration of a test.
	/// </summary>
	private static SqliteDbContext CreateContext()
	{
		DbContextOptions<SqliteDbContext> options = new DbContextOptionsBuilder<SqliteDbContext>()
			.UseSqlite("DataSource=:memory:")
			.AddInterceptors(new SqlitePragmaInterceptor())
			.Options;

		return new(options);
	}

	/// <summary>
	/// Reads the current value of a pragma of the given connection.
	/// </summary>
	private static long ReadPragma(DbConnection connection, string name)
	{
		using DbCommand command = connection.CreateCommand();

		command.CommandText = $"PRAGMA {name};";

		return (long)command.ExecuteScalar()!;
	}
	#endregion
}
