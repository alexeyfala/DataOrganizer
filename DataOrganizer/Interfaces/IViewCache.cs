using System;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Manages the cached controls keyed by arbitrary objects.
/// </summary>
public interface IViewCache
{
	#region Methods
	/// <summary>
	/// Removes the cached control associated with the specified key and disposes its
	/// <see cref="IDisposable" /> data context, if any.
	/// </summary>
	void Remove<T>(T key) where T : notnull;
	#endregion
}
