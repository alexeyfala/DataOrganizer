using System;

namespace DataOrganizer.Interfaces.Diagnostics;

/// <summary>
/// Provides a means of detecting unhandled exceptions in an application.
/// </summary>
public interface IGlobalExceptionHandler : IDisposable
{
	#region Methods
	/// <summary>
	/// Starts tracking unhandled exceptions in the application.
	/// </summary>
	void StartMonitoring();
	#endregion
}
