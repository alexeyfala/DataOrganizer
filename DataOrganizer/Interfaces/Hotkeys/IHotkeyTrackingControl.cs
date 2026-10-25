using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Hotkeys;

/// <summary>
/// Pauses and resumes the hotkey tracking while the application runs.
/// </summary>
public interface IHotkeyTrackingControl
{
	#region Methods
	/// <summary>
	/// Pauses the hotkey tracking, as while the hotkeys of a file are edited.
	/// </summary>
	Task PauseHotkeyTrackingAsync();

	/// <summary>
	/// Starts the hotkey tracking again when the settings turn it on.
	/// </summary>
	void ResumeHotkeyTracking();
	#endregion
}
