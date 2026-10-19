namespace DataOrganizer.Enums.Documents;

/// <summary>
/// What a write of the contents of a file changes, which decides whether the file gets a new date of change.
/// </summary>
public enum FileContentsChange
{
	/// <summary>
	/// The data of the file changed, which gives it a new date of change.
	/// </summary>
	Edit,

	/// <summary>
	/// Only the way the contents are shown changed, which leaves the date as it is.
	/// </summary>
	View
}
