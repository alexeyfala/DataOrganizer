using System;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Manages the cached controls keyed by arbitrary objects.
/// </summary>
public interface IViewCache
{
	#region Methods
	/// <summary>
	/// Removes the cached control associated with the specified key and disposes the control and its data context,
	/// where they are <see cref="IDisposable" />.
	/// </summary>
	void Remove<T>(T key) where T : notnull;
	#endregion
}
