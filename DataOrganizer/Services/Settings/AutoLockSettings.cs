using DataProtection.Interfaces;
using Settings.Interfaces;

namespace DataOrganizer.Services.Settings;

/// <inheritdoc cref="IAutoLockSettings" />
public sealed class AutoLockSettings : IAutoLockSettings
{
	#region Data
	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;
	#endregion

	#region Constructors
	public AutoLockSettings(IAppSettingsStore settingsStore) => _settingsStore = settingsStore;
	#endregion

	#region Properties
	/// <inheritdoc />
	public int AutoLockMinutes => _settingsStore
		.Settings
		.AutoLockMinutes;
	#endregion
}
