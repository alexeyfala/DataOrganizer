namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Adjusts the auto-lock countdown to the shown contents while the application runs.
/// </summary>
public interface IAutoLockControl
{
	#region Methods
	/// <summary>
	/// Runs the auto-lock countdown while decrypted contents are shown and stops it otherwise.
	/// </summary>
	void RefreshAutoLock();

	/// <summary>
	/// Starts the auto-lock countdown over.
	/// </summary>
	void RestartAutoLock();
	#endregion
}
