namespace Repository.DTO;

/// <summary>
/// Parameters for SQlite database backup.
/// </summary>
public readonly struct BackupSqliteParameters
{
	#region Properties
	/// <summary>
	/// Returns <c>True</c> if the connection pool for the database at <see cref="DestFilePath" /> should be cleared.
	/// </summary>
	public required bool ClearDestPool { get; init; }

	/// <summary>
	/// Returns <c>True</c> if the connection pool for the database at <see cref="SourceFilePath" /> should be cleared.
	/// </summary>
	public required bool ClearSourcePool { get; init; }

	/// <summary>
	/// Absolute path to the destination file.
	/// </summary>
	public required string DestFilePath { get; init; }

	/// <summary>
	/// Absolute path to the source file.
	/// </summary>
	public required string SourceFilePath { get; init; }
	#endregion
}
