using Avalonia.Controls;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>ConsoleWindow</c>.
/// </summary>
internal sealed class ConsoleWindowSettings : PositionSizeSettings
{
	#region Properties
	/// <summary>
	/// Font size of the console text.
	/// </summary>
	public required double FontSize { get; init; }

	/// <summary>
	/// <c>True</c> when the window stays on top of the others.
	/// </summary>
	public required bool IsTopmost { get; init; }

	/// <inheritdoc cref="Avalonia.Controls.WindowState" />
	public required WindowState WindowState { get; init; }

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	public required bool WordWrap { get; init; }
	#endregion
}
