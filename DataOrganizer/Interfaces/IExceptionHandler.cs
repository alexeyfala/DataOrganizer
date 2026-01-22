using System;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides a means of detecting unhandled exceptions in an application.
/// </summary>
public interface IExceptionHandler : IDisposable
{
	#region Methods
	/// <summary>
	/// Starts tracking unhandled exceptions in the application.
	/// </summary>
	void StartMonitoring();
	#endregion
}
