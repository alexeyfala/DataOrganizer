using Serilog.Events;
using System.ComponentModel;

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
	[Description("Fill the database with random objects for debugging.")]
	bool FillObjects { get; }

	/// <summary>
	/// Show console window to view logs.
	/// </summary>
	[Description("Show console window to view logs.")]
	bool IsConsoleNeeded { get; }

	/// <summary>
	/// Logging level entries <see cref="LogEventLevel.Debug" />, default <see cref="LogEventLevel.Information" />.
	/// </summary>
	[Description("Logging level entries Debug, default Information.")]
	LogEventLevel MinimumLogEventLevel { get; }

	/// <summary>
	/// Show help information.
	/// </summary>
	[Description("Show help information.")]
	bool PrintHelp { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Returns help information.
	/// </summary>
	string GetHelp();
	#endregion
}
