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
	/// Application name "DataOrganizer".
	/// </summary>
	public static string AppName { get; }

	/// <summary>
	/// Application name "Data Organizer".
	/// </summary>
	public static string AppNameParted { get; }

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

		AppName = attributes
			.First(x => x.Key == "AppName")
			.Value!;

		AppNameParted = attributes
			.First(x => x.Key == "AppNameParted")
			.Value!;
	}
	#endregion

	#region Helpers
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
