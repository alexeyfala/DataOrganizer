using DataOrganizer.Infrastructure.AppInstance.Helpers;
using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using DataOrganizer.Shared.Common;
using System.IO;

namespace DataOrganizer.Infrastructure.AppInstance.Services;

/// <inheritdoc cref="IAppEnvironment" />
public sealed class AppEnvironment : IAppEnvironment
{
	#region Properties
	/// <inheritdoc />
	public string AppDataDirectoryPath { get; }

	/// <inheritdoc />
	public string ClipboardHistoryDirectoryPath { get; }

	/// <inheritdoc />
	public string DatabaseDirectoryPath { get; }

	/// <inheritdoc />
	public string NotepadDirectoryPath { get; }

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
	/// <summary>
	/// Builds the paths of the data folder of the instance, numbered after the instance, and of its subfolders.
	/// </summary>
	public AppEnvironment(IInstanceRegistry instanceRegistry)
	{
		const string directoryName = "Instance";

		_appCount = instanceRegistry.InstanceNumber;

		AppDataDirectoryPath = Path.Combine(
			AppDataDirectory.GetPath(),
			_appCount == 1 ? directoryName : $"{directoryName} ({_appCount})");

		ClipboardHistoryDirectoryPath = Path.Combine(
			AppDataDirectoryPath,
			"ClipboardHistory");

		DatabaseDirectoryPath = Path.Combine(
			AppDataDirectoryPath,
			"Database");

		NotepadDirectoryPath = Path.Combine(
			AppDataDirectoryPath,
			"Notepad");

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
			? AppInfo.AppDisplayName
			: $"{AppInfo.AppDisplayName} ({_appCount})";
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
}
