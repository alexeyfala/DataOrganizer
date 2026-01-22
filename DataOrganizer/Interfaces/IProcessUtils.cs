using System.Diagnostics;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides a means of interacting with <see cref="Process" />.
/// </summary>
public interface IProcessUtils
{
	#region Methods
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
	/// Launches the required utility process and searches the output for the required information.
	/// </summary>
	string? LaunchProcess(string fileName, string arguments, string outputSearchValue);

	/// <summary>
	/// Launches the required utility process and displays the received information.
	/// </summary>
	string[] LaunchProcessGetOutput(string fileName, string arguments);

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
