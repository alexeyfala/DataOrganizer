namespace DataOrganizer.Enums.Views;

/// <summary>
/// State a window is shown in.
/// </summary>
/// <remarks>
/// The numbers are the ones the settings files keep.
/// </remarks>
public enum WindowShowState
{
	/// <summary>
	/// Neither minimized nor maximized.
	/// </summary>
	Normal = 0,

	/// <summary>
	/// Minimized.
	/// </summary>
	Minimized = 1,

	/// <summary>
	/// Filling the working area of its screen.
	/// </summary>
	Maximized = 2,

	/// <summary>
	/// Filling its whole screen, without a frame.
	/// </summary>
	FullScreen = 3
}
