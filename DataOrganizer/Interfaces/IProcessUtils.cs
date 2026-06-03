using System.Diagnostics;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides a means of interacting with <see cref="Process" />.
/// </summary>
public interface IProcessUtils
{
	#region Methods
	/// <summary>
	/// Returns the number of running instances of the application.
	/// </summary>
	int GetAppProcessesCount();

	/// <summary>
	/// Returns a list of child processes.
	/// </summary>
	Process[] GetChildProcesses(int parentProcessId);

	/// <summary>
	/// <c>True</c> when the process with <see cref="Process.Id" /> specified by <paramref name="processId"/> exists.
	/// </summary>
	bool IsProcessExists(int processId);

	/// <inheritdoc cref="Process.Kill()" />
	void KillProcess(int processId);

	/// <summary>
	/// Opens the application directory.
	/// </summary>
	void OpenAppDirectory();

	/// <summary>
	/// Opens a directory.
	/// </summary>
	void OpenDirectory(string directoryPath);

	/// <summary>
	/// Launches a file process depending on the operating system.<br />
	/// When running a file without an extension or when there is no application
	/// in system associated with file <paramref name="processId"/> will have default value.
	/// </summary>
	/// <returns><see cref="Process.Id" /></returns>
	bool StartProcess(string filePath, out int processId);

	/// <summary>
	/// Launches <paramref name="appPath" /> with <paramref name="fileArgument" /> passed
	/// as a single command-line argument. Returns <c>True</c> and the PID of the new
	/// process on success; <c>False</c> with default <paramref name="processId" /> when
	/// the process did not start.
	/// </summary>
	bool StartProcess(string appPath, string fileArgument, out int processId);

	/// <inheritdoc cref="Process.Start(string)" />
	Process StartProcess(string fileName);
	#endregion
}
