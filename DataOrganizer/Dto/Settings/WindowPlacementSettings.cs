using Avalonia.Controls;

namespace DataOrganizer.Dto.Settings;

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

	/// <inheritdoc cref="Avalonia.Controls.WindowState" />
	public required WindowState WindowState { get; init; }
	#endregion
}
