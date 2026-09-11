namespace Repository.Dto;

/// <summary>
/// Parameters of a copy of the database file made through SQLite.
/// </summary>
public readonly struct CopyDatabaseParameters
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the connection pool for the database at <see cref="DestinationFilePath" /> should be cleared.
	/// </summary>
	public required bool ClearDestinationPool { get; init; }

	/// <summary>
	/// <c>True</c> when the connection pool for the database at <see cref="SourceFilePath" /> should be cleared.
	/// </summary>
	public required bool ClearSourcePool { get; init; }

	/// <summary>
	/// Absolute path to the destination file.
	/// </summary>
	public required string DestinationFilePath { get; init; }

	/// <summary>
	/// Absolute path to the source file.
	/// </summary>
	public required string SourceFilePath { get; init; }
	#endregion
}
