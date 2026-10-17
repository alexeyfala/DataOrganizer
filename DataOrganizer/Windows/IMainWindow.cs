namespace DataOrganizer.Windows;

/// <summary>
/// A main window of the application: the editor or the favorites window.
/// </summary>
public interface IMainWindow
{
	#region Properties
	/// <summary>
	/// <c>True</c> while the window closes to give way to the other main window.
	/// </summary>
	bool IsClosingToSwitch { get; set; }
	#endregion

	#region Methods
	/// <summary>
	/// Closes the window.
	/// </summary>
	void Close();
	#endregion
}
