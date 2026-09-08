namespace DataOrganizer.Interfaces;

/// <summary>
/// Speaks to the user: a toast outside the main window and snackbar messages inside it.
/// </summary>
public interface INotificationService
{
	#region Methods
	/// <summary>
	/// Shows a message about a failure inside the main window.
	/// </summary>
	void ShowErrorSnackbar(string text);

	/// <summary>
	/// Shows a message about a completed action inside the main window.
	/// </summary>
	void ShowInformationSnackbar(string text);

	/// <summary>
	/// Shows a notification in a separate window, which needs neither the main window nor its focus.
	/// </summary>
	void ShowToast(string message);

	/// <summary>
	/// Shows a message about something that needs attention inside the main window.
	/// </summary>
	void ShowWarningSnackbar(string text);
	#endregion
}
