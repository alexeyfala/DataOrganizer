using DataOrganizer.Interfaces;
using Shared.Common;
using System;
using System.IO;

namespace DataOrganizer.Services;

public sealed class AppEnvironment : IAppEnvironment
{
	#region Properties
	/// <inheritdoc />
	public string AppDataDirectoryPath { get; }

	/// <inheritdoc />
	public string DatabaseDirectoryPath { get; }

	/// <inheritdoc />
	public string SandboxDirectoryPath { get; }
	#endregion

	#region Data
	/// <summary>
	/// The number of running application instances.
	/// </summary>
	private readonly int _appCount;
	#endregion

	#region Constructors
	public AppEnvironment(IProcessUtils processUtils)
	{
		const string directoryName = "Data";

		_appCount = processUtils.GetAppProcessesCount();

		AppDataDirectoryPath = Path.Combine(
			GetAppDataDirectoryPath(),
			_appCount == 1 ? directoryName : $"{directoryName} ({_appCount})");

		DatabaseDirectoryPath = Path.Combine(
			AppDataDirectoryPath,
			"Database");

		SandboxDirectoryPath = Path.Combine(
			AppDataDirectoryPath,
			"Sandbox");
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public string GetAppInstanceName()
	{
		return _appCount == 1
			? AppUtils.AppName
			: $"{AppUtils.AppName} ({_appCount})";
	}

	/// <inheritdoc />
	public string GetSettingsFilePath(string fileName)
	{
		return Path.Combine(
			AppDataDirectoryPath,
			"Settings",
			fileName + ".json");
	}
	#endregion

	#region Service
	/// <summary>
	/// Returns the path to the application directory.
	/// </summary>
	private static string GetAppDataDirectoryPath()
	{
		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			AppUtils.AppNameInOneWord);
	}
	#endregion
}
