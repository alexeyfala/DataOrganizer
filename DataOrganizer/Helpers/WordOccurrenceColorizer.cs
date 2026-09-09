using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;

namespace DataOrganizer.Helpers;

/// <summary>
/// Paints every occurrence of a word in the editor with the given brush.
/// </summary>
internal sealed class WordOccurrenceColorizer : DocumentColorizingTransformer
{
	#region Data
	/// <summary>
	/// Brush.
	/// </summary>
	private readonly IBrush _brush;

	/// <summary>
	/// Word.
	/// </summary>
	private readonly string _word;
	#endregion

	#region Constructors
	public WordOccurrenceColorizer(string word, IBrush brush)
	{
		_word = word;

		_brush = brush;
	}
	#endregion

	#region Methods
	protected override void ColorizeLine(DocumentLine line)
	{
		int lineStartOffset = line.Offset;

		string text = CurrentContext
			.Document
			.GetText(line);

		int start = 0;

		int index;

		while ((index = text.IndexOf(_word, start, StringComparison.Ordinal)) >= 0)
		{
			ChangeLinePart(
				lineStartOffset + index,
				lineStartOffset + index + _word.Length,
				element => element.BackgroundBrush = _brush);

			start = index + 1;
		}
	}
	#endregion
}
