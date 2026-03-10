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
	/// Returns <c>True</c> if the process with <see cref="Process.Id" /> specified by <paramref name="processId"/> exists.
	/// </summary>
	bool IsProcessExists(int processId);

	/// <inheritdoc cref="Process.Kill()" />
	void KillProcess(in int processId);

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
	/// When running a file without an extension <paramref name="processId"/> will have default value.
	/// </summary>
	/// <returns><see cref="Process.Id" /></returns>
	bool StartProcess(string filePath, out int processId);

	/// <inheritdoc cref="Process.Start(string)" />
	Process StartProcess(string fileName);
	#endregion
}
