namespace AppInstance.Interfaces;

/// <summary>
/// Provides facts about the build the application runs from.
/// </summary>
public interface IBuildInfo
{
	#region Properties
	/// <summary>
	/// <c>True</c> in a Debug build.
	/// </summary>
	bool IsDebug { get; }
	#endregion
}
