using DataOrganizer.Dto.Documents;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Values of a text editor that its saved state keeps.
/// </summary>
public interface IEditorStateValues
{
	#region Properties
	/// <summary>
	/// Language that the text takes when none is chosen; <c>null</c> for plain text.
	/// </summary>
	string? DefaultSyntaxLanguage { get; }

	/// <summary>
	/// Font size of the text.
	/// </summary>
	double FontSize { get; set; }

	/// <summary>
	/// <c>True</c> when line endings are shown.
	/// </summary>
	bool ShowEndOfLine { get; set; }

	/// <summary>
	/// <c>True</c> when spaces are shown.
	/// </summary>
	bool ShowSpaces { get; set; }

	/// <summary>
	/// <c>True</c> when tabs are shown.
	/// </summary>
	bool ShowTabs { get; set; }

	/// <summary>
	/// Language of the text for the syntax highlighting; <c>null</c> for plain text.
	/// </summary>
	string? SyntaxLanguage { get; set; }

	/// <summary>
	/// Caret, selection, scroll position, bookmarks and folded blocks of the text.
	/// </summary>
	DocumentViewState? ViewState { get; set; }

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	bool WordWrap { get; set; }
	#endregion
}
