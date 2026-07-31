using DataOrganizer.Interfaces;
using Shared.Common;

namespace DataOrganizer.Services;

/// <summary>
/// Reports the running application version from the entry assembly.
/// </summary>
public sealed class AppVersionProvider : IAppVersionProvider
{
	#region Properties
	/// <inheritdoc />
	public string? CurrentVersion => AppUtils.AppVersion;
	#endregion
}
