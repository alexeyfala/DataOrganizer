namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted settings of <c>ConsoleWindow</c>.
/// </summary>
internal sealed class ConsoleWindowSettings : WindowPlacementSettings
{
	#region Properties
	/// <summary>
	/// Font size of the console text.
	/// </summary>
	public required double FontSize { get; init; }

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	public required bool WordWrap { get; init; }
	#endregion
}
