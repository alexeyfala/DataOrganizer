namespace Repository.Enums;

/// <summary>
/// The outcome of an attempt to connect to the database.
/// </summary>
public enum DbConnectionStatus
{
	/// <summary>
	/// The database is open and its schema matches the model.
	/// </summary>
	Connected,

	/// <summary>
	/// The file cannot be opened, is not a SQLite database, or is damaged.
	/// </summary>
	FileUnreadable,

	/// <summary>
	/// The schema is older than this version and cannot be brought to the model; the data is intact.
	/// </summary>
	SchemaTooOld,

	/// <summary>
	/// The schema comes from a newer version of the application; the data is intact.
	/// </summary>
	SchemaTooNew
}
