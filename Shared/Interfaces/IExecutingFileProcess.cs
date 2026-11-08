using System;

namespace Shared.Interfaces;

/// <summary>
/// Process that opens or holds a file in the operating system, held so that a kill can only reach it and never a process
/// that took its id.
/// </summary>
public interface IExecutingFileProcess : IDisposable
{
	#region Properties
	/// <summary>
	/// <c>True</c> once the process has ended.
	/// </summary>
	bool HasExited { get; }

	/// <summary>
	/// Id of the process.
	/// </summary>
	int Id { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Kills the process at once.
	/// </summary>
	void Kill();
	#endregion
}
