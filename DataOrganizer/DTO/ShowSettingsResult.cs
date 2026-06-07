using DataOrganizer.DTO.Settings;

namespace DataOrganizer.DTO;

/// <summary>
/// Result of displaying settings.
/// </summary>
public class ShowSettingsResult
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the settings were saved.
	/// </summary>
	public required bool IsSaved { get; init; }

	/// <inheritdoc cref="AppSettings" />
	public required AppSettings Settings { get; init; }
	#endregion
}
