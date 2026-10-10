namespace DataOrganizer.Interfaces.Dialogs;

/// <summary>
/// Closes the dialog the main window currently shows.
/// </summary>
public interface IDialogHostCloser
{
	#region Methods
	/// <summary>
	/// Closes the dialog without a result.
	/// </summary>
	void Close();
	#endregion
}
