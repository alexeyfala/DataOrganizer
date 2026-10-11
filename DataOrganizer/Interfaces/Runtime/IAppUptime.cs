using System;

namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Time the application has been running, counted from the moment its services are built.
/// </summary>
public interface IAppUptime
{
	#region Properties
	/// <summary>
	/// Time passed since the start.
	/// </summary>
	TimeSpan Elapsed { get; }
	#endregion
}
