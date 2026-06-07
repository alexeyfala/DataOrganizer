using Shared.Common;
using System;
using System.IO;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides tools for working with the application environment.
/// </summary>
public interface IAppEnvironment
{
	#region Properties
	/// <summary>
	/// Path to the directory containing application data.
	/// </summary>
	string AppDataDirectoryPath { get; }

	/// <summary>
	/// Application database directory path.
	/// </summary>
	string DatabaseDirectoryPath { get; }

	/// <summary>
	/// Application sandbox directory path.
	/// </summary>
	string SandboxDirectoryPath { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Returns the path to the application directory.
	/// </summary>
	static string GetAppDataDirectoryPath()
	{
		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			$"{(AppUtils.IsDebug ? AppUtils.AppNameAsOneWord + "_Debug" : string.Empty)}");
	}

	/// <summary>
	/// Returns the name of the application based on the number of running instances.
	/// </summary>
	string GetAppInstanceName();

	/// <summary>
	/// Returns the path to a clipboard history file (e.g. the encrypted journal or its wrapped key).
	/// </summary>
	string GetClipboardHistoryFilePath(string fileName);

	/// <summary>
	/// Returns path to the file with settings.
	/// </summary>
	string GetSettingsFilePath(string fileName);
	#endregion
}
