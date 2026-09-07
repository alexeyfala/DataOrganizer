namespace DataOrganizer.Interfaces;

/// <summary>
/// Shows snackbar messages one after another, so that a message is not cut short by the next one.
/// </summary>
public interface ISnackbarService
{
	#region Methods
	/// <summary>
	/// Shows a message about a failure.
	/// </summary>
	void ShowError(string text);

	/// <summary>
	/// Shows a message about a completed action.
	/// </summary>
	void ShowInformation(string text);

	/// <summary>
	/// Shows a message about something that needs attention.
	/// </summary>
	void ShowWarning(string text);
	#endregion
}
