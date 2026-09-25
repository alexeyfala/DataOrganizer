using AvaloniaEdit;
using System.Drawing;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// State of the built-in editor for a file.
/// </summary>
public readonly struct FileEditorState
{
	#region Properties
	/// <summary>
	/// The caret position.
	/// </summary>
	public TextViewPosition CaretPosition { get; init; }

	/// <summary>
	/// Font size.
	/// </summary>
	public double FontSize { get; init; }

	/// <summary>
	/// The offset of scrolling position.
	/// </summary>
	public Point ScrollOffset { get; init; }

	/// <summary>
	/// The length of selected text.
	/// </summary>
	public int SelectionLength { get; init; }

	/// <summary>
	/// The start of selected text.
	/// </summary>
	public int SelectionStart { get; init; }

	/// <summary>
	/// <c>True</c> when line endings are shown.
	/// </summary>
	public bool ShowEndOfLine { get; init; }

	/// <summary>
	/// <c>True</c> when spaces are shown.
	/// </summary>
	public bool ShowSpaces { get; init; }

	/// <summary>
	/// <c>True</c> when tabs are shown.
	/// </summary>
	public bool ShowTabs { get; init; }

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	public bool WordWrap { get; init; }
	#endregion
}
