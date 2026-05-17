using Serilog.Events;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Application installation parameters passed on the command line.
/// </summary>
public interface ICommandLineOptions
{
	#region Properties
	/// <summary>
	/// Fill the database with random objects for debugging.
	/// </summary>
	bool FillObjects { get; }

	/// <summary>
	/// Show console window to view logs.
	/// </summary>
	bool IsConsoleNeeded { get; }

	/// <summary>
	/// Logging level entries <see cref="LogEventLevel.Debug" />, default <see cref="LogEventLevel.Information" />.
	/// </summary>
	LogEventLevel MinimumLogEventLevel { get; }

	/// <summary>
	/// Show help information.
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
