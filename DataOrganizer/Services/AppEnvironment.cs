using DataOrganizer.Interfaces;
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

	#region Constructors
	public AppEnvironment(IProcessUtils processUtils)
	{
		const string directoryName = "Data";

		int appCount = processUtils.GetAppProcessesCount();

		AppDataDirectoryPath = Path.Combine(
			Environment.CurrentDirectory,
			appCount == 1 ? directoryName : $"{directoryName} ({appCount})");

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
	public string GetSettingsFilePath(string fileName)
	{
		return Path.Combine(
			AppDataDirectoryPath,
			"Settings",
			fileName + ".json");
	}
	#endregion
}
