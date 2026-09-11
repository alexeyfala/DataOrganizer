using System.Drawing;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// The stored position and size of a window.
/// </summary>
public class PositionSizeSettings : PositionSettings
{
	#region Properties
	/// <inheritdoc cref="System.Drawing.Size" />
	public required Size Size { get; init; }
	#endregion
}
