using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Repository.Interceptors;
using System.Data.Common;
using System.Threading.Tasks;

namespace Repository.UnitTests.TestTypes;

[TestFixture(Description = $@"Tests of ""{nameof(SqlitePragmas)}"" type")]
internal class SqlitePragmasTests
{
	#region Data
	/// <summary>
	/// Connection string of a database that lives only for the duration of a test.
	/// </summary>
	private const string InMemoryDataSource = "DataSource=:memory:";

	/// <summary>
	/// The value <c>PRAGMA temp_store</c> reports for <c>MEMORY</c>.
	/// </summary>
	private const long TempStoreMemory = 2L;
	#endregion

	#region Methods
	/// <summary>
	/// <see cref="SqlitePragmas.Apply" />: turns secure deletion on and keeps temporary data in memory.
	/// </summary>
	[Test]
	public void Apply_Sets_The_Pragmas_Of_An_Opened_Connection()
	{
		// Arrange
		using SqliteConnection connection = new(InMemoryDataSource);

		connection.Open();

		// Act
		SqlitePragmas.Apply(connection);

		// Assert
		ReadPragma(connection, "secure_delete")
			.Should()
			.Be(1L);

		ReadPragma(connection, "temp_store")
			.Should()
			.Be(TempStoreMemory);
	}

	/// <summary>
	/// <see cref="SqlitePragmas.ApplyAsync" />: turns secure deletion on and keeps temporary data in memory.
	/// </summary>
	[Test]
	public async Task ApplyAsync_Sets_The_Pragmas_Of_An_Opened_Connection()
	{
		// Arrange
		await using SqliteConnection connection = new(InMemoryDataSource);

		await connection.OpenAsync();

		// Act
		await SqlitePragmas.ApplyAsync(connection);

		// Assert
		ReadPragma(connection, "secure_delete")
			.Should()
			.Be(1L);

		ReadPragma(connection, "temp_store")
			.Should()
			.Be(TempStoreMemory);
	}

	/// <summary>
	/// <see cref="SqlitePragmas.Open" />: opens the connection with the pragmas already in place.
	/// </summary>
	[Test]
	public void Open_Opens_The_Connection_And_Sets_The_Pragmas()
	{
		// Arrange
		using SqliteConnection connection = new(InMemoryDataSource);

		// Act
		SqlitePragmas.Open(connection);

		// Assert
		connection
			.State
			.Should()
			.Be(System.Data.ConnectionState.Open);

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
