using Cysharp.Text;
using DataOrganizer.Interfaces;
using Serilog.Events;
using Shared.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

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

		Dictionary<string, string> descriptions = GetCommandDescriptions();

		int maxCommandLength = GetLongestValueLength(descriptions.Keys);

		descriptions
			.OrderBy(x => x.Key)
			.ForEachFor((element, i) =>
			{
				int requiredLength = maxCommandLength - element.Key.Length;

				string spaces = new(' ', requiredLength);

				builder.Append(element.Key);

				builder.Append(": ");

				builder.Append(spaces);

				if (i < descriptions.Count - 1)
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

	#region Helpers
	/// <summary>
	/// Returns descriptions of the command line arguments that the application works with.
	/// </summary>
	private static Dictionary<string, string> GetCommandDescriptions() => new()
	{
		{ ConsoleArg, GetDescription(nameof(ICommandLineOptions.IsConsoleNeeded)) },
		{ DebugArg, GetDescription(nameof(ICommandLineOptions.MinimumLogEventLevel)) },
		{ FillObjectsArg, GetDescription(nameof(ICommandLineOptions.FillObjects)) },
		{ HelpArg, GetDescription(nameof(ICommandLineOptions.PrintHelp)) }
	};

	/// <summary>
	/// Returns the value of the <see cref="DescriptionAttribute" /> applied to
	/// the <see cref="ICommandLineOptions" /> property with the given name.
	/// </summary>
	private static string GetDescription(string propertyName)
	{
		return typeof(ICommandLineOptions)
			.GetProperty(propertyName)?
			.GetCustomAttribute<DescriptionAttribute>()?
			.Description ?? "Missing ...";
	}

	/// <summary>
	/// Returns the longest value.
	/// </summary>
	private static int GetLongestValueLength(IEnumerable<string> values) => (values.MaxBy(x => x.Length)?.Length) ?? 0;
	#endregion
}
