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
	/// Logs a <see cref="LogEventLevel.Debug" /> level entry.
	/// </summary>
	public static void LogDebug(
		this ILogger target,
		string? message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		target.Debug(
			MessageSourceTemplate,
			DecodeUnicode(message, target),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Debug" /> level entry using a template, without calling code information.
	/// </summary>
	public static void LogDebugWithTemplate(this ILogger target, string message) => target.Debug("{0}", message);

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Error" /> level entry.
	/// </summary>
	public static void LogError(
		this ILogger target,
		string message,
		bool isAssertDebug = true,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		target.Error(
			MessageSourceTemplate,
			DecodeUnicode(message, target),
			CreateSourceInfo(filePath, callerName, lineNumber));

		if (!isAssertDebug || AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		Debug.Fail(message);
	}

	/// <summary>
	/// Logs a <see cref="Exception" /> level entry.
	/// </summary>
	public static void LogException(
		this ILogger target,
		Exception exception,
		bool isAssertDebug = true,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		target.Error(
			exception,
			"{Source}",
			CreateSourceInfo(filePath, callerName, lineNumber));

		if (!isAssertDebug || AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		Debug.Fail(exception.Message);
	}

	/// <summary>
	/// Logs a <see cref="Exception" /> level entry.
	/// </summary>
	public static void LogException(
		this ILogger target,
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		target.Error(
			exception,
			MessageSourceTemplate,
			DecodeUnicode(message, target),
			CreateSourceInfo(filePath, callerName, lineNumber));

		if (AppDomain
			.CurrentDomain
			.IsRunningFromNUnit())
		{
			return;
		}

		Debug.Fail(exception.Message);
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Information" /> level entry.
	/// </summary>
	public static void LogInformation(
		this ILogger target,
		string? message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		target.Information(
			MessageSourceTemplate,
			DecodeUnicode(message, target),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}

	/// <summary>
	/// Logs an entry of level <see cref="LogEventLevel.Information" /> using a template, without any information about the calling code.
	/// </summary>
	public static void LogInformationWithTemplate(this ILogger target, string message)
	{
		target.Information("{0}", message);
	}

	/// <summary>
	/// Logs a <see cref="LogEventLevel.Warning" /> level entry.
	/// </summary>
	public static void LogWarning(
		this ILogger target,
		string message,
		[CallerFilePath] string filePath = "",
		[CallerMemberName] string callerName = "",
		[CallerLineNumber] int lineNumber = 0)
	{
		target.Warning(
			MessageSourceTemplate,
			DecodeUnicode(message, target),
			CreateSourceInfo(filePath, callerName, lineNumber));
	}
	#endregion

	#region Service
	/// <summary>
	/// Creates a message with information about the event source.
	/// </summary>
	private static string CreateSourceInfo(
		string filePath,
		string callerName,
		int lineNumber) => $"{callerName} {lineNumber} {Path.GetFileName(AppUtils.GetPlatformEntryPath(filePath))}";

	/// <summary>
	/// Converts Unicode characters into readable ones.
	/// </summary>
	private static string? DecodeUnicode(string? value, ILogger logger)
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

	[GeneratedRegex(@"\\u(?<Value>[a-zA-Z0-9]{4})", RegexOptions.Compiled)]
	private static partial Regex UnicodeCharRegex();
	#endregion
}
