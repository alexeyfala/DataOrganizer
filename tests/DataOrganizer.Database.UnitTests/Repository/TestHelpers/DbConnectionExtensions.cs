using System.Data.Common;

namespace DataOrganizer.Database.UnitTests.Repository.TestHelpers;

/// <summary>
/// Settings of a database connection that a test reads.
/// </summary>
internal static class DbConnectionExtensions
{
	#region Methods
	/// <summary>
	/// Reads the current value of a pragma of the given connection.
	/// </summary>
	public static long ReadPragma(this DbConnection connection, string name)
	{
		using DbCommand command = connection.CreateCommand();

		command.CommandText = $"PRAGMA {name};";

		return (long)command.ExecuteScalar()!;
	}
	#endregion
}
