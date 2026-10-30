using System.Diagnostics.CodeAnalysis;

namespace Shared.Common;

/// <summary>
/// Defaults applied to log output across the application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Holds the format of the log")]
public static class LogDefaults
{
	#region Properties
	/// <summary>
	/// Time format for logging.
	/// </summary>
	public static string TimestampFormat => "HH:mm:ss.fff";
	#endregion
}
