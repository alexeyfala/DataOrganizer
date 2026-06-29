using Shared.Enums;
using Shared.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Common;

public static class AppUtils
{
	#region Properties
	/// <summary>
	/// Application name "Data Organizer".
	/// </summary>
	public static string AppName { get; }

	/// <summary>
	/// Application name "DataOrganizer" in one word.
	/// </summary>
	public static string AppNameAsOneWord { get; }

	/// <summary>
	/// Application version.
	/// </summary>
	public static string? AppVersion { get; } = Assembly
		.GetEntryAssembly()?
		.GetVersionWithSuffix() ?? "unknown";

	/// <summary>
	/// Current operating system.
	/// </summary>
	public static OperatingSystemType CurrentOs { get; } = GetCurrentOs();

	/// <inheritdoc cref="IsDebugMode" />
	public static bool IsDebug { get; } = IsDebugMode();

	/// <summary>
	/// <c>True</c> when the current operating system is <see cref="OperatingSystemType.Linux" />.
	/// </summary>
	public static bool IsLinux { get; } = CurrentOs == OperatingSystemType.Linux;

	/// <summary>
	/// <c>True</c> when the current operating system is <see cref="OperatingSystemType.MacOs" />.
	/// </summary>
	public static bool IsMacOs { get; } = CurrentOs == OperatingSystemType.MacOs;

	/// <inheritdoc cref="IsReleaseMode" />
	public static bool IsRelease { get; } = IsReleaseMode();

	/// <summary>
	/// <c>True</c> when the current operating system is <see cref="OperatingSystemType.Windows" />.
	/// </summary>
	public static bool IsWindows { get; } = CurrentOs == OperatingSystemType.Windows;

	/// <summary>
	/// Json serialization options.
	/// </summary>
	public static JsonSerializerOptions JsonOptions { get; } = new()
	{
		WriteIndented = true,
		ReferenceHandler = ReferenceHandler.IgnoreCycles
	};

	/// <summary>
	/// Time format for logging.
	/// </summary>
	public static string LogTimestampFormat => "HH:mm:ss.fff";

	/// <summary>
	/// The name of the program for opening files depending on the operating system.
	/// </summary>
	public static string PlatformSpecificExplorer { get; } = GetPlatformSpecificExplorer();

	/// <summary>
	/// Delay in milliseconds for displaying the tip.
	/// </summary>
	public static int TipDelay => 400;

	/// <summary>
	/// File extension ".txt".
	/// </summary>
	public static string TxtExtension => ".txt";
	#endregion

	#region Data
	/// <summary>
	/// The extension of SQLite database file with dot.
	/// </summary>
	public const string SQLiteExtension = ".sqlite";
	#endregion

	#region Constructors
	static AppUtils()
	{
		AssemblyMetadataAttribute[] attributes = [.. Assembly
			.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyMetadataAttribute>()];

		AppName = attributes
			.First(x => x.Key == "AppName")
			.Value!;

		AppNameAsOneWord = attributes
			.First(x => x.Key == "AppNameAsOneWord")
			.Value!;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Generates a random string in upper case of the required length.
	/// </summary>
	public static string CreateRandomString(int length)
	{
		const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

		char[] output = [.. Enumerable
			.Repeat(alphabet, length)
			.Select(x => x[Random.Shared.Next(x.Length)])];

		return new string(output);
	}

	/// <summary>
	/// Performs a transformation on a file system object path obtained using <see cref="CallerFilePathAttribute" />.
	/// </summary>
	/// <remarks>
	/// The value obtained using <see cref="CallerFilePathAttribute" /> passed at compile time
	/// and depends on the type of computer on which the code is compiled.
	/// Compiling code for <see cref="OperatingSystemType.Linux" /> happens in <see cref="OperatingSystemType.Windows" />,
	/// so it is necessary to replace the '\' characters in the path to <see cref="Path.DirectorySeparatorChar" />.
	/// </remarks>
	public static string GetPlatformEntryPath(string filePath)
	{
		return !IsLinux
			? filePath
			: filePath.Replace('\\', Path.DirectorySeparatorChar);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns a value for <see cref="CurrentOs" />.
	/// </summary>
	private static OperatingSystemType GetCurrentOs()
	{
		if (OperatingSystem.IsWindows())
		{
			return OperatingSystemType.Windows;
		}

		if (OperatingSystem.IsLinux())
		{
			return OperatingSystemType.Linux;
		}

		return OperatingSystem.IsMacOS()
			? OperatingSystemType.MacOs
			: OperatingSystemType.Unknown;
	}

	/// <summary>
	/// Returns the value for <see cref="PlatformSpecificExplorer" />.
	/// </summary>
	private static string GetPlatformSpecificExplorer() => CurrentOs switch
	{
		OperatingSystemType.Windows => "explorer",
		OperatingSystemType.Linux => "xdg-open",
		OperatingSystemType.MacOs => "open",
		_ => throw new NotImplementedException()
	};

	/// <summary>
	/// Allows you to determine whether the application is in debug mode.
	/// </summary>
	private static bool IsDebugMode()
	{
		bool value = false;

		Determine(ref value);

		return value;

		[Conditional("DEBUG")]
		static void Determine(ref bool value) => value = true;
	}

	/// <summary>
	/// <c>True</c> when the application is in release mode.
	/// </summary>
	private static bool IsReleaseMode()
	{
		bool value = false;

		Determine(ref value);

		return value;

		[Conditional("RELEASE")]
		static void Determine(ref bool value) => value = true;
	}
	#endregion
}
