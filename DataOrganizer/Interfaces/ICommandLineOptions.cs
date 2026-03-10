using Serilog.Events;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Application installation parameters passed on the command line.
/// </summary>
public interface ICommandLineOptions
{
	#region Properties
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
	#endregion

	#region Methods
	/// <summary>
	/// Returns help information.
	/// </summary>
	string GetHelp();
	#endregion
}
