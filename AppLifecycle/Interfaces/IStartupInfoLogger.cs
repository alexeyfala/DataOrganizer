namespace AppLifecycle.Interfaces;

/// <summary>
/// Writes the facts a launch log starts with.
/// </summary>
public interface IStartupInfoLogger
{
	#region Methods
	/// <summary>
	/// Writes the command line help when it is asked for, the version, the system and the application settings.
	/// </summary>
	void Log();
	#endregion
}
