using Settings.Enums;

namespace Settings.Dto;

/// <summary>
/// The stored placement of a window: its position, size and state, and whether it stays on top.
/// </summary>
public class WindowPlacementSettings : PositionSizeSettings
{
	#region Properties
	/// <summary>
	/// <c>True</c> when the window stays above the windows of the other applications.
	/// </summary>
	public required bool IsTopmost { get; init; }

	/// <inheritdoc cref="WindowShowState" />
	public required WindowShowState WindowState { get; init; }
	#endregion
}
