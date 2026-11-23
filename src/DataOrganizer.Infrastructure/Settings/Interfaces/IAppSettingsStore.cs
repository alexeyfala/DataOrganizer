using DataOrganizer.Infrastructure.Settings.Dto;

namespace DataOrganizer.Infrastructure.Settings.Interfaces;

/// <summary>
/// Holds the application settings and persists them in a file.
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
	/// Saves <see cref="Settings" /> in a file.
	/// </summary>
	void Save();
	#endregion
}
