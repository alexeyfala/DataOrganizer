using Serilog;
using Serilog.Events;
using Shared.Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Shared.Extensions;

public static partial class SerilogExtensions
{
	#region Data
	/// <summary>
	/// Template for writing a message with a source.
	/// </summary>
	private const string MessageSourceTemplate = "{Message} \u2192 {Source}";
	#endregion

	#region Methods
	/// <summary>
	/// Returns a message containing information about the event source.
	/// </summary>
	public static string GetSourceInfo(
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0) => CreateSourceInfo(filePath, callerName, lineNumber);

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Debug" /> level entry, building the interpolated message only when Debug logging is enabled.
	/// </summary>
	public static void LogDebug(
		this ILogger logger,
		[InterpolatedStringHandlerArgument(nameof(logger))]
		LogDebugInterpolatedStringHandler message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		if (!logger.IsEnabled(LogEventLevel.Debug))
		{
			return;
		}

		logger.Debug(
			MessageSourceTemplate,
			DecodeUnicode(message.ToStringAndClear(), logger),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Debug" /> level entry from an already-computed message.
	/// </summary>
	public static void LogDebug(
		this ILogger logger,
		string? message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		if (!logger.IsEnabled(LogEventLevel.Debug))
		{
			return;
		}

		logger.Debug(
			MessageSourceTemplate,
			DecodeUnicode(message, logger),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}

	/// <summary>
	/// Logs an entry of level <see cref="LogEventLevel.Debug" /> using a template,
	/// without any information about the calling code.
	/// </summary>
	public static void LogDebugWithTemplate(
		this ILogger logger,
		[InterpolatedStringHandlerArgument(nameof(logger))]
		LogDebugInterpolatedStringHandler message)
	{
		if (!logger.IsEnabled(LogEventLevel.Debug))
		{
			return;
		}

		logger.Debug("{0}", message.ToStringAndClear());
	}

	/// <summary>
	/// Logs an entry of level <see cref="LogEventLevel.Debug" /> using a template,
	/// without any information about the calling code.
	/// </summary>
	public static void LogDebugWithTemplate(
		this ILogger logger,
		string message)
	{
		if (!logger.IsEnabled(LogEventLevel.Debug))
		{
			return;
		}

		logger.Debug("{0}", message);
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Error" /> level entry.
	/// </summary>
	public static void LogError(
		this ILogger logger,
		string message,
		bool breakInDebugger = true,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		logger.Error(
			MessageSourceTemplate,
			DecodeUnicode(message, logger),
			CreateSourceInfo(filePath, callerName, lineNumber));

		if (!breakInDebugger || AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		Debugger.Break();
	}

	/// <summary>
	/// Logs a <see cref="Exception" /> level entry.
	/// </summary>
	public static void LogException(
		this ILogger logger,
		Exception exception,
		bool breakInDebugger = true,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		logger.Error(
			exception,
			"{Source}",
			CreateSourceInfo(filePath, callerName, lineNumber));

		if (!breakInDebugger || AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		Debugger.Break();
	}

	/// <summary>
	/// Logs a <see cref="Exception" /> level entry.
	/// </summary>
	public static void LogException(
		this ILogger logger,
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		logger.Error(
			exception,
			MessageSourceTemplate,
			DecodeUnicode(message, logger),
			CreateSourceInfo(filePath, callerName, lineNumber));

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		Debugger.Break();
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Information" /> level entry.
	/// </summary>
	public static void LogInformation(
		this ILogger logger,
		string? message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		logger.Information(
			MessageSourceTemplate,
			DecodeUnicode(message, logger),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}

	/// <summary>
	/// Logs an entry of level <see cref="LogEventLevel.Information" /> using a template, without any information about the calling code.
	/// </summary>
	public static void LogInformationWithTemplate(this ILogger logger, string message)
	{
		logger.Information("{0}", message);
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Warning" /> level entry.
	/// </summary>
	public static void LogWarning(
		this ILogger logger,
		string message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		logger.Warning(
			MessageSourceTemplate,
			DecodeUnicode(message, logger),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}

	/// <summary>
	/// Converts Unicode characters into readable ones.
	/// </summary>
	internal static string? DecodeUnicode(string? value, ILogger logger)
	{
		try
		{
			return value is not null
				? UnicodeCharRegex().Replace(value, x => ((char)int.Parse(x.Groups[nameof(Capture.Value)].Value, NumberStyles.HexNumber)).ToString())
				: value;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{Source}", GetSource());

			static string GetSource(
				[CallerFilePath] string filePath = "",
				[CallerMemberName] string callerName = "",
				[CallerLineNumber] int lineNumber = 0) => CreateSourceInfo(filePath, callerName, lineNumber);
		}

		return value;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Creates a message with information about the event source.
	/// </summary>
	private static string CreateSourceInfo(
		string filePath,
		string callerName,
		int lineNumber) => $"{callerName} {lineNumber} {Path.GetFileName(PlatformInfo.NormalizeSourcePath(filePath))}";

	[GeneratedRegex(@"\\u(?<Value>[a-zA-Z0-9]{4})", RegexOptions.Compiled)]
	private static partial Regex UnicodeCharRegex();
	#endregion
}
