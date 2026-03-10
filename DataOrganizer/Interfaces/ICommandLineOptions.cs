using Serilog.Events;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Application installation parameters passed on the command line.
/// </summary>
public interface ICommandLineOptions
{
	#region Properties
	/// <summary>
	/// Path to the directory containing application data.
	/// </summary>
	string AppDataDirectoryPath { get; }

	/// <summary>
	/// Application database directory path.
	/// </summary>
	string DatabaseDirectoryPath { get; }

	/// <summary>
	/// Indicates the need to fill the database with random objects for debugging.
	/// </summary>
	bool FillObjects { get; }

	/// <summary>
	/// Indicates whether the console should be shown.
	/// </summary>
	bool IsConsoleNeeded { get; }

	/// <summary>
	/// Minimum logging level.
	/// </summary>
	LogEventLevel MinimumLogEventLevel { get; }

	/// <summary>
	/// Indicates the need to display the help information.
	/// </summary>
	bool PrintHelp { get; }

	/// <summary>
	/// Application sandbox directory path.
	/// </summary>
	string SandboxDirectoryPath { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Returns help information.
	/// </summary>
	string GetHelp();

	/// <summary>
	/// Returns path to the file with settings.
	/// </summary>
	string GetSettingsFilePath(string fileName);
	#endregion
}
