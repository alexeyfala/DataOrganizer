using Cysharp.Text;
using DataOrganizer.Interfaces;
using Serilog.Events;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataOrganizer.Services;

/// <inheritdoc cref="ICommandLineOptions" />
public sealed class CommandLineOptions : ICommandLineOptions
{
	#region Properties
	/// <inheritdoc />
	public string AppDataDirectoryPath { get; }

	/// <inheritdoc />
	public string DatabaseDirectoryPath { get; }

	/// <inheritdoc />
	public bool FillObjects { get; }

	/// <inheritdoc />
	public bool IsConsoleNeeded { get; }

	/// <inheritdoc />
	public LogEventLevel MinimumLogEventLevel { get; }

	/// <inheritdoc />
	public bool PrintHelp { get; }

	/// <inheritdoc />
	public string SandboxDirectoryPath { get; }
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
	private static readonly Dictionary<string, string> _commandDescriptions = new()
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
		AppDataDirectoryPath = Environment.CurrentDirectory;

		DatabaseDirectoryPath = Path.Combine(AppDataDirectoryPath, "Database");

		FillObjects = args.Contains(FillObjectsArg);

		IsConsoleNeeded = args.Contains(ConsoleArg);

		MinimumLogEventLevel = args.Contains(DebugArg)
			? LogEventLevel.Debug
			: LogEventLevel.Information;

		PrintHelp = args.Contains(HelpArg);

		SandboxDirectoryPath = Path.Combine(
			AppDataDirectoryPath,
			"Sandbox");
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public string GetHelp()
	{
		using Utf16ValueStringBuilder builder = ZString.CreateStringBuilder();

		builder.AppendLine("Available command line arguments:");

		int maxCommandLength = GetMaxCommandLength();

		_commandDescriptions
			.OrderBy(x => x.Key)
			.ToArray()
			.ForEachFor((element, i) =>
			{
				int requiredLength = maxCommandLength - element.Key.Length;

				string spaces = new([.. GetSpaces(requiredLength)]);

				builder.Append(element.Key);

				builder.Append(": ");

				builder.Append(spaces);

				if (i < _commandDescriptions.Count - 1)
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

	/// <inheritdoc />
	public string GetSettingsFilePath(string fileName)
	{
		return Path.Combine(
			AppDataDirectoryPath,
			"Settings",
			fileName + ".json");
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns the number of characters in the longest command from <see cref="_commandDescriptions" />.
	/// </summary>
	private static int GetMaxCommandLength()
	{
		return (_commandDescriptions.Keys.MaxBy(x => x.Length)?.Length) ?? 0;
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
