using DataOrganizer.Services;

namespace DataOrganizer.Extensions;

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
	/// Appends the argument <see cref="CommandLineOptions.FillObjectsArg" /> to the array.
	/// </summary>
	public static string[] AddFillObjectsArg(this string[] args) => [.. args, CommandLineOptions.FillObjectsArg];

	/// <summary>
	/// Appends the argument <see cref="CommandLineOptions.HelpArg" /> to the array.
	/// </summary>
	public static string[] AddHelpArg(this string[] args) => [.. args, CommandLineOptions.HelpArg];
	#endregion
}
