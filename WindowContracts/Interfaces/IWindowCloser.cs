namespace WindowContracts.Interfaces;

/// <summary>
/// Closes the open windows of the desktop application.
/// </summary>
public interface IWindowCloser
{
	#region Methods
	/// <summary>
	/// Closes every open window.
	/// </summary>
	void CloseAllWindows();
	#endregion
}
