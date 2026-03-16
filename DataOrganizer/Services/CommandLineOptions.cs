using Cysharp.Text;
using DataOrganizer.Interfaces;
using Serilog.Events;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Services;

/// <inheritdoc cref="ICommandLineOptions" />
public sealed class CommandLineOptions : ICommandLineOptions
{
	#region Properties
	/// <inheritdoc />
	public bool FillObjects { get; }

	/// <inheritdoc />
	public bool IsConsoleNeeded { get; }

	/// <inheritdoc />
	public LogEventLevel MinimumLogEventLevel { get; }

	/// <inheritdoc />
	public bool PrintHelp { get; }
	#endregion

	#region Data
	/// <inheritdoc cref="IsConsoleNeeded" />
	internal const string ConsoleArg = "--console";

	/// <inheritdoc cref="MinimumLogEventLevel" />
	internal const string DebugArg = "--debug";

	/// <inheritdoc cref="FillObjects" />
	internal const string FillObjectsArg = "--fillobjects";

	/// <inheritdoc cref="PrintHelp" />
	internal const string HelpArg = "--help";

	/// <summary>
	/// Contains descriptions of the command line arguments that the application works with.
	/// </summary>
	private static readonly Dictionary<string, string> CommandDescriptions = new()
	{
		{ ConsoleArg, "Show console window to view logs." },
		{ DebugArg, $@"Logging level entries ""{LogEventLevel.Debug}"", default ""{LogEventLevel.Information}""." },
		{ FillObjectsArg, "Fill the database with random objects for debugging." },
		{ HelpArg, "Show help information." }
	};
	#endregion

	#region Constructors
	public CommandLineOptions(string[] args)
	{
		FillObjects = args.Contains(FillObjectsArg);

		IsConsoleNeeded = args.Contains(ConsoleArg);

		MinimumLogEventLevel = args.Contains(DebugArg)
			? LogEventLevel.Debug
			: LogEventLevel.Information;

		PrintHelp = args.Contains(HelpArg);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public string GetHelp()
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.AppendLine("Available command line arguments:");

		int maxCommandLength = GetMaxCommandLength();

		CommandDescriptions
			.OrderBy(x => x.Key)
			.ToArray()
			.ForEachFor((element, i) =>
			{
				int requiredLength = maxCommandLength - element.Key.Length;

				string spaces = new([.. GetSpaces(requiredLength)]);

				builder.Append(element.Key);

				builder.Append(": ");

				builder.Append(spaces);

				if (i < CommandDescriptions.Count - 1)
				{
					builder.AppendLine(element.Value);
				}
				else
				{
					builder.Append(element.Value);
				}
			});

		return builder.ToString();
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns the number of characters in the longest command from <see cref="CommandDescriptions" />.
	/// </summary>
	private static int GetMaxCommandLength()
	{
		return (CommandDescriptions.Keys.MaxBy(x => x.Length)?.Length) ?? 0;
	}

	/// <summary>
	/// Returns a specified number of spaces.
	/// </summary>
	private static IEnumerable<char> GetSpaces(int count)
	{
		for (int i = 0; i < count; i++)
		{
			yield return ' ';
		}
	}
	#endregion
}
