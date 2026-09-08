namespace DataOrganizer.Enums;

/// <summary>
/// Outcome of writing a converted folder to the database.
/// </summary>
public enum UpdateDatabaseResult
{
	/// <summary>
	/// The transaction was rejected; the database is rolled back to the copy taken before the conversion.
	/// </summary>
	FailedToSaveInDb,

	/// <summary>
	/// The write failed with an exception; the database is rolled back to the copy taken before the conversion.
	/// </summary>
	ExceptionThrown,

	/// <summary>
	/// The conversion is stored.
	/// </summary>
	Done
}
