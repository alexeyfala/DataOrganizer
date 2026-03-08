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
	/// Path to the directory containing application data.
	/// </summary>
	public static string AppDataDirectoryPath { get; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		AppNameInOneWord);

	/// <summary>
	/// Application name "Data Organizer".
	/// </summary>
	public static string AppName => "Data Organizer";

	/// <summary>
	/// Application name "DataOrganizer" in one word.
	/// </summary>
	public static string AppNameInOneWord => "DataOrganizer";

	/// <summary>
	/// Application version.
	/// </summary>
	public static string? AppVersion { get; } = Assembly.GetEntryAssembly().GetVersionWithSuffix();

	/// <summary>
	/// Current operating system.
	/// </summary>
	public static OperateSystem CurrentOs { get; } = GetCurrentOs();

	/// <summary>
	/// Application database directory name.
	/// </summary>
	public static string Database => nameof(Database);

	/// <summary>
	/// Application database directory path.
	/// </summary>
	public static string DatabaseDirectoryPath { get; } = IsDebugMode()
		? Path.Combine(AppDataDirectoryPath, Database)
		: Path.Combine(Environment.CurrentDirectory, Database);

	/// <summary>
	/// Returns <c>True</c> if the current operating system is <see cref="OperateSystem.Linux" />.
	/// </summary>
	public static bool IsLinux { get; } = CurrentOs == OperateSystem.Linux;

	/// <summary>
	/// Returns <c>True</c> if the current operating system is <see cref="OperateSystem.MacOs" />.
	/// </summary>
	public static bool IsMacOs { get; } = CurrentOs == OperateSystem.MacOs;

	/// <summary>
	/// Returns <c>True</c> if the current operating system is <see cref="OperateSystem.Windows" />.
	/// </summary>
	public static bool IsWindows { get; } = CurrentOs == OperateSystem.Windows;

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
	public static string PlatformSpecificExplorer { get; }

	/// <summary>
	/// Application sandbox directory name.
	/// </summary>
	public static string Sandbox => nameof(Sandbox);

	/// <summary>
	/// Application sandbox directory path.
	/// </summary>
	public static string SandboxDirectoryPath { get; } = Path.Combine(
		AppDataDirectoryPath,
		Sandbox);

	/// <summary>
	/// Application settings directory name.
	/// </summary>
	public static string Settings => nameof(Settings);

	/// <summary>
	/// Delay in millideconds for displaying the tip.
	/// </summary>
	public static int TipDelay => 400;
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
		// This property must be initialized in the constructor, not directly,
		// otherwise, a System.TypeInitializationException exception is thrown.
		PlatformSpecificExplorer = GetPlatformSpecificExplorer();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Generates a random file name.
	/// </summary>
	public static string CreateRandomFileName(in int length)
	{
		return $"{CreateRandomString(length)}_file.{CreateRandomString(3).ToLower()}";
	}

	/// <summary>
	/// Generates a random string in upper case of the required length.
	/// </summary>
	public static string CreateRandomString(in int length)
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
	/// Compiling code for <see cref="OperateSystem.Linux" /> happens in <see cref="OperateSystem.Windows" />,
	/// so it is necessary to replace the '\' characters in the path to <see cref="Path.DirectorySeparatorChar" />.
	/// </remarks>
	public static string GetPlatformEntryPath(string filePath)
	{
		return !IsLinux
			? filePath
			: filePath.Replace('\\', Path.DirectorySeparatorChar);
	}

	/// <summary>
	/// Returns path to the file with settings.
	/// </summary>
	public static string GetSettingsFilePath(string fileName)
	{
		return Path.Combine(
			AppDataDirectoryPath,
			Settings,
			fileName + ".json");
	}

	/// <summary>
	/// Allows you to determine whether the application is in debug mode.
	/// </summary>
	public static bool IsDebugMode()
	{
		bool value = false;

		Determine(ref value);

		return value;

		[Conditional("DEBUG")]
		// ReSharper disable once RedundantAssignment
		static void Determine(ref bool value) => value = true;
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns a value for <see cref="CurrentOs" />.
	/// </summary>
	private static OperateSystem GetCurrentOs()
	{
		if (OperatingSystem.IsWindows())
		{
			return OperateSystem.Windows;
		}

		if (OperatingSystem.IsLinux())
		{
			return OperateSystem.Linux;
		}

		return OperatingSystem.IsMacOS()
			? OperateSystem.MacOs
			: OperateSystem.Unknown;
	}

	/// <summary>
	/// Returns the value for <see cref="PlatformSpecificExplorer" />.
	/// </summary>
	private static string GetPlatformSpecificExplorer() => CurrentOs switch
	{
		OperateSystem.Windows => "explorer",
		OperateSystem.Linux => "xdg-open",
		OperateSystem.MacOs => "open",
		_ => throw new NotImplementedException()
	};
	#endregion
}
