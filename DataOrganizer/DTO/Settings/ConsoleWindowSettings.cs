using Avalonia.Controls;

namespace DataOrganizer.DTO.Settings;

internal sealed class ConsoleWindowSettings : PositionSizeSettings
{
	#region Properties
	/// <summary>
	/// Font size.
	/// </summary>
	public required double FontSize { get; init; }

	/// <summary>
	/// On top of all windows.
	/// </summary>
	public required bool IsTopmost { get; init; }

	/// <summary>
	/// Indicates the need to wrap words.
	/// </summary>
	public required bool IsWordWrap { get; init; }

	/// <inheritdoc cref="Avalonia.Controls.WindowState" />
	public required WindowState WindowState { get; init; }
	#endregion
}
