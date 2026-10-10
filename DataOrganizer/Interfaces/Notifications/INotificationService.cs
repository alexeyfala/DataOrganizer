using DataOrganizer.Helpers;

namespace DataOrganizer.Interfaces.Notifications;

/// <summary>
/// Speaks to the user: a toast outside the windows and snackbar messages inside them.
/// </summary>
public interface INotificationService
{
	#region Methods
	/// <summary>
	/// Shows a message about a failure inside a window, by default the main one.
	/// </summary>
	void ShowErrorSnackbar(string text, string snackbarHostIdentifier = SnackbarHostIdentifiers.Main);

	/// <summary>
	/// Shows a message about a completed action inside a window, by default the main one.
	/// </summary>
	void ShowInformationSnackbar(string text, string snackbarHostIdentifier = SnackbarHostIdentifiers.Main);

	/// <summary>
	/// Shows a notification in a separate window, which needs neither the main window nor its focus.
	/// </summary>
	void ShowToast(string message);

	/// <summary>
	/// Shows a message about something that needs attention inside a window, by default the main one.
	/// </summary>
	void ShowWarningSnackbar(string text, string snackbarHostIdentifier = SnackbarHostIdentifiers.Main);
	#endregion
}
