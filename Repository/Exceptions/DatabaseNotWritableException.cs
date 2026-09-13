using Repository.Enums;
using System;

namespace Repository.Exceptions;

/// <summary>
/// A change was asked of a database that does not accept writes.
/// </summary>
public sealed class DatabaseNotWritableException : Exception
{
	#region Constructors
	public DatabaseNotWritableException()
	{
	}

	public DatabaseNotWritableException(string message)
		: base(message)
	{
	}

	public DatabaseNotWritableException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	public DatabaseNotWritableException(DbConnectionStatus status, string operationName)
		: base($"{operationName} is refused: the database is {status}.")
	{
	}
	#endregion
}
