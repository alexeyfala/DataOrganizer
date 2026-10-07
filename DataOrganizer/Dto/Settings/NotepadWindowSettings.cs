using Avalonia.Controls;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>NotepadWindow</c>.
/// </summary>
public sealed class NotepadWindowSettings : PositionSizeSettings
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
