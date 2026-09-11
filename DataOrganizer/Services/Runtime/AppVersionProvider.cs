using DataOrganizer.Interfaces.Runtime;
using Shared.Common;

namespace DataOrganizer.Services.Runtime;

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
