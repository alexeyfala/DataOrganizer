namespace DataOrganizer.Shared.Interfaces;

/// <summary>
/// The running process of the application.
/// </summary>
public interface IAppProcess
{
	#region Properties
	/// <summary>
	/// Folder the files of the application are in.
	/// </summary>
	string BaseDirectory { get; }

	/// <summary>
	/// Name of the entry assembly; <c>null</c> when it is unknown.
	/// </summary>
	string? EntryAssemblyName { get; }

	/// <summary>
	/// Path of the executable that started the process; <c>null</c> when it is unknown.
	/// </summary>
	string? ProcessPath { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Value of the environment variable <paramref name="name" />; <c>null</c> when it is not set.
	/// </summary>
	string? GetEnvironmentVariable(string name);
	#endregion
}
