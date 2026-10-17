using DataOrganizer.Dto.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace DataOrganizer.Interfaces.Runtime;

/// <summary>
/// Adjusts the background services to the settings and to the shown contents while the application runs.
/// </summary>
public interface IBackgroundServiceControl
{
	#region Methods
	/// <summary>
	/// Applies saved settings, given what the replaced settings had on.
	/// </summary>
	Task ApplySettingsAsync(
		AppSettings settings,
		bool wasPersistingClipboard,
		int previousAutoLockMinutes,
		CancellationToken token = default);

	/// <summary>
	/// Pauses the hotkey tracking, as while the hotkeys of a file are edited.
	/// </summary>
	Task PauseHotkeyTrackingAsync();

	/// <summary>
	/// Runs the auto-lock countdown while decrypted contents are shown and stops it otherwise.
	/// </summary>
	void RefreshAutoLock();

	/// <summary>
	/// Starts the auto-lock countdown over.
	/// </summary>
	void RestartAutoLock();

	/// <summary>
	/// Starts the hotkey tracking again when the settings turn it on.
	/// </summary>
	void ResumeHotkeyTracking();
	#endregion
}
