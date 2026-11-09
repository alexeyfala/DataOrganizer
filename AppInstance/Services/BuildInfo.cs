using AppInstance.Interfaces;
using Shared.Common;
using System.Diagnostics.CodeAnalysis;

namespace AppInstance.Services;

/// <summary>
/// Reports the build configuration of the running application.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Depends on the build configuration")]
public sealed class BuildInfo : IBuildInfo
{
	#region Properties
	/// <inheritdoc />
	public bool IsDebug => AppInfo.IsDebug;
	#endregion
}
