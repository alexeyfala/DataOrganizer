using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Repository.Interceptors;
using Repository.UnitTests.Helpers;
using Shared.Common;
using System;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
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
	/// <see cref="SqlitePragmas.Open" />: the contents of a deleted row do not stay in the database file.
	/// </summary>
	[Test]
	public void Open_Leaves_No_Trace_Of_A_Deleted_Row()
	{
		// Arrange
		using TempSqliteFile file = new();

		string marker = AppUtils.CreateRandomString(64);

		using (SqliteConnection connection = file.Open())
		{
			TempSqliteFile.Execute(connection, "CREATE TABLE Payloads (Id INTEGER PRIMARY KEY, Payload TEXT);");

			using (SqliteCommand command = connection.CreateCommand())
			{
				command.CommandText = "INSERT INTO Payloads (Payload) VALUES ($payload);";

				command
					.Parameters
					.AddWithValue("$payload", string.Concat(Enumerable.Repeat(marker, 50)));

				command.ExecuteNonQuery();
			}

			// Act
			TempSqliteFile.Execute(connection, "DELETE FROM Payloads;");
		}

		// Assert
		byte[] contents = File.ReadAllBytes(file.FilePath);

		contents
			.AsSpan()
			.IndexOf(Encoding.UTF8.GetBytes(marker))
			.Should()
			.Be(-1);
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
