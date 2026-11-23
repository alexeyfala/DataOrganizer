namespace DataOrganizer.Infrastructure.Diagnostics.Interfaces;

/// <summary>
/// Provides a means of detecting unhandled exceptions in an application.
/// </summary>
public interface IGlobalExceptionHandler
{
	#region Methods
	/// <summary>
	/// Starts tracking unhandled exceptions in the application.
	/// </summary>
	void StartMonitoring();
	#endregion
}
