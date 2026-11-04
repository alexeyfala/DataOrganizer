using Shared.Enums;
using System.Runtime.Versioning;

namespace Shared.Interfaces;

/// <summary>
/// Operating system the application runs on.
/// </summary>
public interface IPlatform
{
	#region Properties
	/// <summary>
	/// Current operating system.
	/// </summary>
	OperatingSystemKind CurrentOS { get; }

	/// <summary>
	/// <c>True</c> on Linux.
	/// </summary>
	[SupportedOSPlatformGuard("linux")]
	bool IsLinux { get; }

	/// <summary>
	/// <c>True</c> on macOS.
	/// </summary>
	[SupportedOSPlatformGuard("macos")]
	bool IsMacOS { get; }

	/// <summary>
	/// <c>True</c> on Windows.
	/// </summary>
	[SupportedOSPlatformGuard("windows")]
	bool IsWindows { get; }
	#endregion
}
