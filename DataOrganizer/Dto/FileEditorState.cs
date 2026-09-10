using AvaloniaEdit;
using System.Drawing;

namespace DataOrganizer.Dto;

/// <summary>
/// State of the built-in editor for a file.
/// </summary>
public readonly struct FileEditorState
{
	#region Properties
	/// <summary>
	/// The caret position.
	/// </summary>
	public required TextViewPosition CaretPosition { get; init; }

	/// <summary>
	/// Font size.
	/// </summary>
	public required double FontSize { get; init; }

	/// <summary>
	/// The offset of scrolling position.
	/// </summary>
	public required Point ScrollOffset { get; init; }

	/// <summary>
	/// The length of selected text.
	/// </summary>
	public required int SelectionLength { get; init; }

	/// <summary>
	/// The start of selected text.
	/// </summary>
	public required int SelectionStart { get; init; }

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	public required bool WordWrap { get; init; }
	#endregion
}
