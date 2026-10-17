namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Ends the application.
/// </summary>
public interface IAppShutdown
{
	#region Methods
	/// <summary>
	/// Ends the desktop application, or the process where there is no desktop.
	/// </summary>
	void Shutdown();
	#endregion
}
