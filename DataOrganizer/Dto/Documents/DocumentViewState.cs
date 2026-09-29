using Avalonia;
using AvaloniaEdit;
using Generator.Equals;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Caret, selection, scroll position, bookmarks and folded blocks of a document in the editor.
/// </summary>
[Equatable]
public readonly partial record struct DocumentViewState
{
	#region Properties
	/// <summary>
	/// Numbers of the bookmarked lines; <c>null</c> stands for none, and an empty set is kept as <c>null</c>.
	/// </summary>
	[OrderedEquality]
	public int[]? Bookmarks
	{
		get;
		// One form of none keeps the states without bookmarks equal.
		init => field = value is { Length: > 0 } ? value : null;
	}

	/// <summary>
	/// The caret position.
	/// </summary>
	public required TextViewPosition CaretPosition { get; init; }

	/// <summary>
	/// Offsets where the folded blocks start, while the other blocks are unfolded; <c>null</c> stands for none, and an
	/// empty set is kept as <c>null</c>.
	/// </summary>
	[OrderedEquality]
	public int[]? FoldedBlocks
	{
		get;
		// One form of none keeps the states without folded blocks equal.
		init => field = value is { Length: > 0 } ? value : null;
	}

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

	/// <summary>
	/// Offsets where the unfolded blocks start, while the other blocks are folded; an empty set folds every block, and
	/// <c>null</c> leaves the blocks to <see cref="FoldedBlocks" />.
	/// </summary>
	[OrderedEquality]
	public int[]? UnfoldedBlocks { get; init; }
	#endregion
}
