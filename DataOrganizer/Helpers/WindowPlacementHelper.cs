using Avalonia;

namespace DataOrganizer.Helpers;

/// <summary>
/// Window coordinate arithmetic over the working area of a screen.
/// </summary>
internal static class WindowPlacementHelper
{
	#region Data
	/// <summary>
	/// Default gap between a corner placed window and the edges of the working area, in pixels.
	/// </summary>
	public const int CornerMargin = 10;
	#endregion

	#region Methods
	public static PixelPoint GetLowerRightPosition(
		PixelRect workingArea,
		PixelSize windowSize,
		int margin = CornerMargin)
	{
		return new PixelPoint(
			workingArea.X + workingArea.Width - (windowSize.Width + margin),
			workingArea.Y + workingArea.Height - (windowSize.Height + margin));
	}
	#endregion
}
