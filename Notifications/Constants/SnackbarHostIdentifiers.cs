namespace Notifications.Constants;

/// <summary>
/// Identifiers of the snackbar hosts, one for each window that shows messages inside it.
/// </summary>
public static class SnackbarHostIdentifiers
{
	#region Data
	/// <summary>
	/// Host of the main window, the editor or the favorites.
	/// </summary>
	public const string Main = nameof(Main);

	/// <summary>
	/// Host of the notepad.
	/// </summary>
	public const string Notepad = nameof(Notepad);
	#endregion
}
