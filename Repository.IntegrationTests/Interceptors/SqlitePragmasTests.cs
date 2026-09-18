using AwesomeAssertions;
using Microsoft.Data.Sqlite;
using Repository.IntegrationTests.Fixtures;
using Repository.Interceptors;
using Shared.Common;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Repository.IntegrationTests.Interceptors;

[TestFixture(Description = $@"Tests of ""{nameof(SqlitePragmas)}"" type")]
internal class SqlitePragmasTests
{
	#region Methods
	/// <summary>
	/// <see cref="SqlitePragmas.OpenConnection" />: the contents of a deleted row do not stay in the database file.
	/// </summary>
	[Test]
	public void OpenConnection_Leaves_No_Trace_Of_A_Deleted_Row()
	{
		// Arrange
		using TempSqliteFile file = new();

		string marker = RandomString.Create(64);

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
	#endregion
}
