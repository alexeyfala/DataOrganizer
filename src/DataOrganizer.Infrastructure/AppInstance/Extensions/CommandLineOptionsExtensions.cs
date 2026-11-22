using DataOrganizer.Infrastructure.AppInstance.Services;

namespace DataOrganizer.Infrastructure.AppInstance.Extensions;

/// <summary>
/// Appends the arguments that <see cref="CommandLineOptions" /> knows to an array of command line arguments.
/// </summary>
public static class CommandLineOptionsExtensions
{
	#region Methods
	/// <summary>
	/// Appends the argument <see cref="CommandLineOptions.ConsoleArg" /> to the array.
	/// </summary>
	public static string[] AddConsoleArg(this string[] args) => [.. args, CommandLineOptions.ConsoleArg];

	/// <summary>
	/// Appends the argument <see cref="CommandLineOptions.DebugArg" /> to the array.
	/// </summary>
	public static string[] AddDebugArg(this string[] args) => [.. args, CommandLineOptions.DebugArg];

	/// <summary>
	/// Appends the argument <see cref="CommandLineOptions.HelpArg" /> to the array.
	/// </summary>
	public static string[] AddHelpArg(this string[] args) => [.. args, CommandLineOptions.HelpArg];
	#endregion
}
