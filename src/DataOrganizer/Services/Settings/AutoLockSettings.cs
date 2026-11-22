using DataOrganizer.Infrastructure.Settings.Interfaces;
using DataOrganizer.Security.Interfaces;

namespace DataOrganizer.Services.Settings;

/// <inheritdoc cref="IAutoLockSettings" />
public sealed class AutoLockSettings : IAutoLockSettings
{
	#region Data
	/// <inheritdoc cref="IAppSettingsStore" />
	private readonly IAppSettingsStore _settingsStore;
	#endregion

	#region Constructors
	/// <summary>
	/// Initializes a new instance of the <see cref="AutoLockSettings" /> class.
	/// </summary>
	public AutoLockSettings(IAppSettingsStore settingsStore) => _settingsStore = settingsStore;
	#endregion

	#region Properties
	/// <inheritdoc />
	public int AutoLockMinutes => _settingsStore
		.Settings
		.AutoLockMinutes;
	#endregion
}
