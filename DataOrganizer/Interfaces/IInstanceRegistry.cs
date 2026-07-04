using System;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Tracks concurrently running application instances and assigns each a distinct number.
/// </summary>
public interface IInstanceRegistry : IDisposable
{
	#region Properties
	/// <summary>
	/// The number of the current instance, starting at 1. Reused once a previous instance exits.
	/// </summary>
	int InstanceNumber { get; }
	#endregion
}
