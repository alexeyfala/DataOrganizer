using DataOrganizer.DTO.Settings;

namespace DataOrganizer.Interfaces;

/// <summary>
/// Holds the application settings and persists them in file.
/// </summary>
public interface IAppSettingsStore
{
	#region Properties
	/// <inheritdoc cref="AppSettings" />
	AppSettings Settings { get; }
	#endregion

	#region Methods
	/// <summary>
	/// Overwrites <see cref="Settings" /> from <paramref name="value" />.
	/// </summary>
	void Overwrite(AppSettings value);

	/// <summary>
	/// Saves <see cref="Settings" /> in file.
	/// </summary>
	void Save();
	#endregion
}
