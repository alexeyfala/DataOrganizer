namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Ends the process of the application.
/// </summary>
public interface IProcessTerminator
{
	#region Methods
	/// <summary>
	/// Ends the process with the given exit code.
	/// </summary>
	void Terminate(int exitCode = 0);
	#endregion
}
