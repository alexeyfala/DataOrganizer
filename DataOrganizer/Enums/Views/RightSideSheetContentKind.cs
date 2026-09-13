namespace DataOrganizer.Enums.Views;

/// <summary>
/// The content in the right side sheet of the editor.
/// </summary>
public enum RightSideSheetContentKind
{
	/// <summary>
	/// The side sheet is closed.
	/// </summary>
	None,

	/// <summary>
	/// The contents copied to the clipboard.
	/// </summary>
	CopyHistory,

	/// <summary>
	/// The files opened in an external application.
	/// </summary>
	ExecutingFiles
}
