using Avalonia;
using AvaloniaEdit;

namespace DataOrganizer.Dto;

/// <summary>
/// Caret, selection and scroll position of a document in the editor.
/// </summary>
public readonly record struct DocumentViewState
{
	#region Properties
	/// <summary>
	/// The caret position.
	/// </summary>
	public required TextViewPosition CaretPosition { get; init; }

	/// <summary>
	/// The offset of scrolling position.
	/// </summary>
	public required Vector ScrollOffset { get; init; }

	/// <summary>
	/// The length of selected text.
	/// </summary>
	public required int SelectionLength { get; init; }

	/// <summary>
	/// The start of selected text.
	/// </summary>
	public required int SelectionStart { get; init; }
	#endregion
}
