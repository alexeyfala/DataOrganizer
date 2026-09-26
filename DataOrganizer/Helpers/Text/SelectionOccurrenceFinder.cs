using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Finds the occurrences of the selected text by the rules common to the highlight and the scroll marks.
/// </summary>
internal static class SelectionOccurrenceFinder
{
	#region Methods
	/// <summary>
	/// Finds the occurrences of the selected text between two offsets of the document, the selection itself included.
	/// </summary>
	public static IEnumerable<SimpleSegment> Find(
		TextDocument document,
		Selection selection,
		int start,
		int end)
	{
		// Only a piece of one line is looked for.
		if (selection is RectangleSelection
			|| selection.IsMultiline
			|| selection.SurroundingSegment is not { Length: > 0 } selected)
		{
			yield break;
		}

		string pattern = document.GetText(selected);

		if (string.IsNullOrWhiteSpace(pattern))
		{
			yield break;
		}

		// A selected word matches whole words only, any other piece matches inside words too.
		bool isWord = pattern.All(IsWordPart) && HasWordBoundaries(document, selected.Offset, selected.Length);

		string text = document.GetText(start, end - start);

		int index = 0;

		while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			int offset = start + index;

			if (!isWord || HasWordBoundaries(document, offset, pattern.Length))
			{
				yield return new SimpleSegment(offset, pattern.Length);
			}

			index += pattern.Length;
		}
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns <c>true</c> when no word character adjoins the segment.
	/// </summary>
	private static bool HasWordBoundaries(TextDocument document, int offset, int length)
	{
		int end = offset + length;

		return (offset == 0 || !IsWordPart(document.GetCharAt(offset - 1)))
			&& (end == document.TextLength || !IsWordPart(document.GetCharAt(end)));
	}

	/// <summary>
	/// Returns <c>true</c> for a character of a word.
	/// </summary>
	private static bool IsWordPart(char c)
	{
		return TextUtilities.GetCharacterClass(c) is CharacterClass.IdentifierPart or CharacterClass.CombiningMark;
	}
	#endregion
}
