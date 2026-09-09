using Shared.Enums;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Shared.Common;

/// <summary>
/// Facts about the operating system the application runs on.
/// </summary>
public static class PlatformInfo
{
	#region Properties
	/// <summary>
	/// Current operating system.
	/// </summary>
	public static OperatingSystemKind CurrentOs { get; } = GetCurrentOs();

	/// <summary>
	/// The name of the program for opening files depending on the operating system.
	/// </summary>
	public static string FileOpener { get; } = GetFileOpener();
	#endregion

	#region Methods
	/// <summary>
	/// Performs a transformation on a file system object path obtained using <see cref="CallerFilePathAttribute" />.
	/// </summary>
	/// <remarks>
	/// The value obtained using <see cref="CallerFilePathAttribute" /> passed at compile time
	/// and depends on the type of computer on which the code is compiled.
	/// Compiling code for <see cref="OperatingSystemKind.Linux" /> happens in <see cref="OperatingSystemKind.Windows" />,
	/// so it is necessary to replace the '\' characters in the path to <see cref="Path.DirectorySeparatorChar" />.
	/// </remarks>
	public static string GetEntryPath(string filePath)
	{
		return !OperatingSystem.IsLinux()
			? filePath
			: filePath.Replace('\\', Path.DirectorySeparatorChar);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns a value for <see cref="CurrentOs" />.
	/// </summary>
	private static OperatingSystemKind GetCurrentOs()
	{
		if (OperatingSystem.IsWindows())
		{
			return OperatingSystemKind.Windows;
		}

		if (OperatingSystem.IsLinux())
		{
			return OperatingSystemKind.Linux;
		}

		return OperatingSystem.IsMacOS()
			? OperatingSystemKind.MacOs
			: OperatingSystemKind.Unknown;
	}

	/// <summary>
	/// Returns the value for <see cref="FileOpener" />.
	/// </summary>
	private static string GetFileOpener() => CurrentOs switch
	{
		OperatingSystemKind.Windows => "explorer",
		OperatingSystemKind.Linux => "xdg-open",
		OperatingSystemKind.MacOs => "open",
		_ => throw new NotImplementedException()
	};
	#endregion
}
