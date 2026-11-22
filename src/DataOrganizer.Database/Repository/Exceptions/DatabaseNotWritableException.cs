using DataOrganizer.Database.Repository.Enums;
using System;
using System.Diagnostics.CodeAnalysis;

namespace DataOrganizer.Database.Repository.Exceptions;

/// <summary>
/// A change was asked of a database that does not accept writes.
/// </summary>
public sealed class DatabaseNotWritableException : Exception
{
	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="DatabaseNotWritableException" /> class.
	/// </summary>
	public DatabaseNotWritableException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DatabaseNotWritableException" /> class.
	/// </summary>
	public DatabaseNotWritableException(string message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DatabaseNotWritableException" /> class.
	/// </summary>
	public DatabaseNotWritableException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// Creates the exception with a message that names the refused operation and the state of the database.
	/// </summary>
	[ExcludeFromCodeCoverage(Justification = "Only words the message")]
	public DatabaseNotWritableException(DbConnectionStatus status, string operationName)
		: base($"{operationName} is refused: the database is {status}.")
	{
	}
	#endregion
}
