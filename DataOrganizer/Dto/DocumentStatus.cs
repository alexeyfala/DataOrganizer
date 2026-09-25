using DataOrganizer.Enums;

namespace DataOrganizer.Dto;

/// <summary>
/// Caret position, selection and lines of a document in the editor.
/// </summary>
public readonly record struct DocumentStatus
{
	#region Properties
	/// <summary>
	/// The caret column, counted from one; a tab takes one column.
	/// </summary>
	public required int Column { get; init; }

	/// <summary>
	/// The caret line, counted from one.
	/// </summary>
	public required int Line { get; init; }

	/// <summary>
	/// The number of lines.
	/// </summary>
	public required int LineCount { get; init; }

	/// <summary>
	/// The line break style.
	/// </summary>
	public required LineEnding LineEnding { get; init; }

	/// <summary>
	/// The number of selected characters.
	/// </summary>
	public required int SelectionLength { get; init; }
	#endregion
}
