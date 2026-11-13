using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Shared.Common;

namespace DataOrganizer.Infrastructure.AppInstance.Services;

/// <summary>
/// Reports the running application version from the entry assembly.
/// </summary>
public sealed class AppVersionProvider : IAppVersionProvider
{
	#region Properties
	/// <inheritdoc />
	public string? CurrentVersion => AppInfo.AppVersion;
	#endregion
}
