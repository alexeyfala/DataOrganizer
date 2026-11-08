using System;
using System.Collections.Generic;

namespace Shared.Interfaces;

/// <summary>
/// Runs a process to its end within a time limit.
/// </summary>
public interface IProcessRunner
{
	#region Methods
	/// <summary>
	/// Runs <paramref name="fileName" /> without a window, each of <paramref name="arguments" /> as one argument; its exit
	/// code, or <c>null</c> when it did not end within <paramref name="timeout" /> and was killed with its children.
	/// </summary>
	int? Run(string fileName, IReadOnlyList<string> arguments, TimeSpan timeout);
	#endregion
}
