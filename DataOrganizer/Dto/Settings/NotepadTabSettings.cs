using DataOrganizer.Dto.Documents;

namespace DataOrganizer.Dto.Settings;

/// <summary>
/// Persisted tab of <c>NotepadWindow</c>.
/// </summary>
public sealed record NotepadTabSettings
{
	#region Properties
	/// <summary>
	/// State of the editor of the tab with the encoding of its text, or <c>null</c> for the defaults.
	/// </summary>
	public required FileEditorState? EditorState { get; init; }

	/// <summary>
	/// Name given to the tab, or <c>null</c>.
	/// </summary>
	public required string? Name { get; init; }

	/// <summary>
	/// Number of the tab.
	/// </summary>
	public required int Number { get; init; }

	/// <summary>
	/// Split of the text: the share of the height of the upper half, or <c>null</c> when it is not split.
	/// </summary>
	public required double? Split { get; init; }
	#endregion
}
