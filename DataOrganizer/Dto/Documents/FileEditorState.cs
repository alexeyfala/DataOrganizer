using AvaloniaEdit;
using System.Drawing;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// State of the built-in editor for a file.
/// </summary>
public readonly struct FileEditorState
{
	#region Data
	/// <summary>
	/// Value of <see cref="SyntaxLanguage" /> for a text kept plain, although the extension of its file has a grammar.
	/// </summary>
	public const string PlainTextLanguage = "plaintext";
	#endregion

	#region Properties
	/// <summary>
	/// Numbers of the bookmarked lines.
	/// </summary>
	public int[]? Bookmarks { get; init; }

	/// <summary>
	/// The caret position.
	/// </summary>
	public TextViewPosition CaretPosition { get; init; }

	/// <summary>
	/// Web name of the encoding chosen for the text; <c>null</c> when the text takes the one found from the contents.
	/// </summary>
	public string? Encoding { get; init; }

	/// <summary>
	/// Offsets where the folded blocks start, while the other blocks are unfolded.
	/// </summary>
	public int[]? FoldedBlocks { get; init; }

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
	/// Language chosen for the syntax highlighting; <c>null</c> when the text takes the one of the file extension.
	/// </summary>
	public string? SyntaxLanguage { get; init; }

	/// <summary>
	/// Offsets where the unfolded blocks start, while the other blocks are folded; an empty set folds every block.
	/// </summary>
	public int[]? UnfoldedBlocks { get; init; }

	/// <summary>
	/// <c>True</c> when long lines are wrapped.
	/// </summary>
	public bool WordWrap { get; init; }
	#endregion
}
