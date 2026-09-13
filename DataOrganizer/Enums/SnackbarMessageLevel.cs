namespace DataOrganizer.Enums;

/// <summary>
/// How urgent a snackbar message is; its colours follow the level.
/// </summary>
public enum SnackbarMessageLevel
{
	/// <summary>
	/// An action that has completed.
	/// </summary>
	Information,

	/// <summary>
	/// Something that needs attention but did not fail.
	/// </summary>
	Warning,

	/// <summary>
	/// An action that failed.
	/// </summary>
	Error
}
