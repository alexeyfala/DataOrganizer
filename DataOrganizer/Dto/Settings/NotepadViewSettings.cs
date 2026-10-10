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
	/// Tabs in their order.
	/// </summary>
	public required NotepadTabSettings[] Tabs { get; init; }
	#endregion
}
