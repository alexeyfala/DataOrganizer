using DataOrganizer.Abstract;
using DataOrganizer.DTO.Settings;

namespace DataOrganizer.DTO;

/// <summary>
/// Result of displaying settings.
/// </summary>
public class ShowSettingsResult : IsSavedBase
{
	#region Properties
	/// <inheritdoc cref="AppSettings" />
	public required AppSettings Settings { get; init; }
	#endregion
}
