namespace DataOrganizer.Interfaces.Settings;

/// <summary>
/// Holds the state of the settings view that lives only for the current application session
/// and is never written to the settings file.
/// </summary>
public interface ISettingsSessionState
{
	#region Properties
	/// <summary>
	/// Index of the settings category opened last.
	/// </summary>
	int LastCategoryIndex { get; set; }
	#endregion
}
