namespace DataOrganizer.Interfaces.Notifications;

/// <summary>
/// Shows notifications in a window of their own.
/// </summary>
public interface IToastNotifier
{
	#region Methods
	/// <summary>
	/// Shows a notification in a separate window, which needs neither the main window nor its focus.
	/// </summary>
	void ShowToast(string message);
	#endregion
}
