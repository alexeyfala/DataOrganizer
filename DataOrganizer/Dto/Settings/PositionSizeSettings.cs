using System.Drawing;

namespace DataOrganizer.Dto.Settings;

public class PositionSizeSettings : PositionSettings
{
	#region Properties
	/// <inheritdoc cref="System.Drawing.Size" />
	public required Size Size { get; init; }
	#endregion
}
