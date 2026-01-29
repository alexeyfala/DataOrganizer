using Material.Colors;
using Material.Styles.Themes.Base;

namespace DataOrganizer.DTO.Settings;

/// <summary>
/// Application settings.
/// </summary>
/// <remarks>
/// <see cref="AppSettings" /> is a record for automatic implementation of equality methods.
/// </remarks>
public record AppSettings
{
	#region Properties
	/// <summary>
	/// Application language.
	/// </summary>
	public required string Language { get; set; }

	/// <summary>
	/// Material design primary color.
	/// </summary>
	public required PrimaryColor PrimaryColor { get; set; }

	/// <summary>
	/// Material design secondary color.
	/// </summary>
	public required SecondaryColor SecondaryColor { get; set; }

	/// <summary>
	/// Theme.
	/// </summary>
	public required BaseThemeMode Theme { get; set; }

	/// <summary>
	/// Indicates whether hotkey tracking should be enabled.
	/// </summary>
	public bool TrackHotkeys { get; set; }	
	#endregion
}
