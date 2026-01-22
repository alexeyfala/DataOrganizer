namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides methods for notifying the user.
/// </summary>
public interface INotificationService
{
	#region Methods
	/// <summary>
	/// Shows a toast notification.
	/// </summary>
	void ShowToast(string message);
	#endregion
}
