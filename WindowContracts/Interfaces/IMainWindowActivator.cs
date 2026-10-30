namespace WindowContracts.Interfaces;

/// <summary>
/// Brings the shown main window forward.
/// </summary>
public interface IMainWindowActivator
{
	#region Methods
	/// <summary>
	/// Brings the main window back from the minimized state and activates it.
	/// </summary>
	void ActivateMainWindow();

	/// <summary>
	/// Brings the main window to the center of the screen of the notepad.
	/// </summary>
	void CenterMainWindow();
	#endregion
}
