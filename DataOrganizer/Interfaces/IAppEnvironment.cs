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
	/// Returns the name of the application based on the number of running instances.
	/// </summary>
	string GetAppInstanceName();

	/// <summary>
	/// Returns path to the file with settings.
	/// </summary>
	string GetSettingsFilePath(string fileName);
	#endregion
}
