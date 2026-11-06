using System.Collections.Generic;

namespace Shared.Interfaces;

/// <summary>
/// Starts processes without waiting for them to end.
/// </summary>
public interface IProcessLauncher
{
	#region Methods
	/// <summary>
	/// Opens <paramref name="filePath" /> in the program the operating system assigns to it; <c>null</c> when no process
	/// of its own started, as for a file the system hands to a program already running.
	/// </summary>
	IExecutingFileProcess? OpenFile(string filePath);

	/// <summary>
	/// Opens <paramref name="filePath" /> in <paramref name="appPath" />, passed to it as a single command-line argument;
	/// <c>null</c> when the process did not start.
	/// </summary>
	IExecutingFileProcess? OpenFile(string appPath, string filePath);

	/// <summary>
	/// Opens <paramref name="target" />, a file or a URL, with the program the operating system assigns to it.
	/// </summary>
	void OpenWithShell(string target);

	/// <summary>
	/// Starts <paramref name="fileName" />, passing each of <paramref name="arguments" /> as one command-line argument.
	/// </summary>
	void StartProcess(string fileName, IReadOnlyList<string> arguments);
	#endregion
}
