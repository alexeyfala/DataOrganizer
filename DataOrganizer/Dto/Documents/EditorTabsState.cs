using DataOrganizer.Dto.Entities;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Files open in the tabs of the editor, with the selected one and the one Ctrl+Tab goes back to.
/// </summary>
public sealed class EditorTabsState
{
	#region Properties
	/// <summary>
	/// Files of the tabs in their order.
	/// </summary>
	public required FileDto[] Files { get; init; }

	/// <summary>
	/// File of the tab Ctrl+Tab goes back to.
	/// </summary>
	public required FileDto? PreviousFile { get; init; }

	/// <summary>
	/// File of the selected tab.
	/// </summary>
	public required FileDto? SelectedFile { get; init; }
	#endregion
}
