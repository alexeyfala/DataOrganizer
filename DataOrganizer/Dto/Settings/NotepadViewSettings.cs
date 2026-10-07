namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted tabs of <c>NotepadWindow</c>.
/// </summary>
public sealed class NotepadViewSettings
{
	#region Properties
	/// <summary>
	/// Number of the selected tab.
	/// </summary>
	public required int? SelectedTabNumber { get; init; }

	/// <summary>
	/// Numbers of the tabs in their order.
	/// </summary>
	public required int[] TabNumbers { get; init; }
	#endregion
}
