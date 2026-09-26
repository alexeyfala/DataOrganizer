using Avalonia;
using AvaloniaEdit;
using System;
using System.Linq;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Caret, selection, scroll position and bookmarks of a document in the editor.
/// </summary>
public readonly record struct DocumentViewState
{
	#region Properties
	/// <summary>
	/// Numbers of the bookmarked lines; <c>null</c> stands for none.
	/// </summary>
	public int[]? Bookmarks { get; init; }

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

	#region Methods
	/// <inheritdoc />
	public bool Equals(DocumentViewState other)
	{
		// The bookmarks compare by their lines rather than by the array, so the same lines taken again make the same state.
		return CaretPosition.Equals(other.CaretPosition)
			&& ScrollOffset.Equals(other.ScrollOffset)
			&& SelectionLength == other.SelectionLength
			&& SelectionStart == other.SelectionStart
			&& (Bookmarks ?? []).SequenceEqual(other.Bookmarks ?? []);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		HashCode hash = new();

		hash.Add(CaretPosition);

		hash.Add(ScrollOffset);

		hash.Add(SelectionLength);

		hash.Add(SelectionStart);

		foreach (int line in Bookmarks ?? [])
		{
			hash.Add(line);
		}

		return hash.ToHashCode();
	}
	#endregion
}
