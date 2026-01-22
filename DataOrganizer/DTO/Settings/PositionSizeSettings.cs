using System.Drawing;

namespace DataOrganizer.DTO.Settings;

public class PositionSizeSettings : PositionSettings
{
	#region Properties
	/// <inheritdoc cref="System.Drawing.Size" />
	public required Size Size { get; init; }
	#endregion
}
