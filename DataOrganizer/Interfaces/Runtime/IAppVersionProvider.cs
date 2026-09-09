namespace DataOrganizer.Interfaces;

/// <summary>
/// Provides the running application version.
/// </summary>
public interface IAppVersionProvider
{
	#region Properties
	/// <summary>
	/// Version of the running application.
	/// </summary>
	string? CurrentVersion { get; }
	#endregion
}
