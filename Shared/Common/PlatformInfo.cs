using Shared.Enums;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Shared.Common;

/// <summary>
/// Facts about the operating system the application runs on.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Depends on the operating system the tests run on")]
public static class PlatformInfo
{
	#region Properties
	/// <summary>
	/// The name of the program for opening files depending on the operating system.
	/// </summary>
	public static string FileOpener { get; } = GetFileOpener();
	#endregion

	#region Methods
	/// <summary>
	/// Brings a path taken from <see cref="CallerFilePathAttribute" /> to the separators of the running platform.
	/// </summary>
	/// <remarks>
	/// The value obtained using <see cref="CallerFilePathAttribute" /> passed at compile time
	/// and depends on the type of computer on which the code is compiled.
	/// Compiling code for <see cref="OperatingSystemKind.Linux" /> happens in <see cref="OperatingSystemKind.Windows" />,
	/// so it is necessary to replace the '\' characters in the path to <see cref="Path.DirectorySeparatorChar" />.
	/// </remarks>
	public static string NormalizeSourcePath(string filePath)
	{
		return !OperatingSystem.IsLinux()
			? filePath
			: filePath.Replace('\\', Path.DirectorySeparatorChar);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the value for <see cref="FileOpener" />.
	/// </summary>
	private static string GetFileOpener()
	{
		if (OperatingSystem.IsWindows())
		{
			return "explorer";
		}

		if (OperatingSystem.IsLinux())
		{
			return "xdg-open";
		}

		return OperatingSystem.IsMacOS()
			? "open"
			: throw new NotImplementedException();
	}
	#endregion
}
