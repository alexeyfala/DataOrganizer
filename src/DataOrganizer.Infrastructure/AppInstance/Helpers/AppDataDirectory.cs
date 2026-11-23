using DataOrganizer.Shared.Common;
using System;
using System.IO;

namespace DataOrganizer.Infrastructure.AppInstance.Helpers;

/// <summary>
/// Locates the directory of the application data.
/// </summary>
internal static class AppDataDirectory
{
	#region Methods
	/// <summary>
	/// Returns the path to the application directory.
	/// </summary>
	public static string GetPath()
	{
		string appDirectory = AppInfo.IsDebug
			? AppInfo.AppName + "_Debug"
			: AppInfo.AppName;

		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			appDirectory);
	}
	#endregion
}
