using Shared.Extensions;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Shared.Common;

/// <summary>
/// Facts about the running application.
/// </summary>
public static class AppInfo
{
	#region Properties
	/// <summary>
	/// Application name shown to the user: "Data Organizer".
	/// </summary>
	public static string AppDisplayName { get; }

	/// <summary>
	/// Application name "DataOrganizer".
	/// </summary>
	public static string AppName { get; }

	/// <summary>
	/// Application version.
	/// </summary>
	public static string? AppVersion { get; } = Assembly
		.GetEntryAssembly()?
		.GetVersionWithSuffix() ?? "unknown";

	/// <inheritdoc cref="IsDebugMode" />
	public static bool IsDebug { get; } = IsDebugMode();

	/// <inheritdoc cref="IsReleaseMode" />
	public static bool IsRelease { get; } = IsReleaseMode();
	#endregion

	#region Constructors
	static AppInfo()
	{
		AssemblyMetadataAttribute[] attributes = [.. Assembly
			.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyMetadataAttribute>()];

		AppDisplayName = attributes
			.First(x => x.Key == "AppDisplayName")
			.Value!;

		AppName = attributes
			.First(x => x.Key == "AppName")
			.Value!;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// <c>True</c> when the application is in debug mode.
	/// </summary>
	private static bool IsDebugMode()
	{
		bool isDebug = false;

		Determine(ref isDebug);

		return isDebug;

		[Conditional("DEBUG")]
		static void Determine(ref bool isDebug) => isDebug = true;
	}

	/// <summary>
	/// <c>True</c> when the application is in release mode.
	/// </summary>
	private static bool IsReleaseMode()
	{
		bool isRelease = false;

		Determine(ref isRelease);

		return isRelease;

		[Conditional("RELEASE")]
		static void Determine(ref bool isRelease) => isRelease = true;
	}
	#endregion
}
