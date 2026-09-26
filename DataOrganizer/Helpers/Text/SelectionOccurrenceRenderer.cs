using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Paints the occurrences of the selected text in the visible lines of the editor.
/// </summary>
internal sealed class SelectionOccurrenceRenderer : IBackgroundRenderer
{
	#region Data
	/// <summary>
	/// Text area with the selection to look for.
	/// </summary>
	private readonly TextArea _textArea;
	#endregion

	#region Constructors
	public SelectionOccurrenceRenderer(TextArea textArea)
	{
		_textArea = textArea;

		// The engine repaints the view only when the selection changes in or above the visible lines.
		_textArea.SelectionChanged += TextArea_SelectionChanged;
	}
	#endregion

	#region Properties
	/// <inheritdoc />
	public KnownLayer Layer => KnownLayer.Background;
	#endregion

	#region Methods
	/// <inheritdoc />
	public void Draw(TextView textView, DrawingContext drawingContext)
	{
		foreach (SimpleSegment occurrence in FindOccurrences(textView))
		{
			// A builder per occurrence keeps the occurrences on adjacent lines from joining into one shape.
			BackgroundGeometryBuilder builder = new()
			{
				AlignToWholePixels = true
			};

			builder.AddSegment(textView, occurrence);

			if (builder.CreateGeometry() is not { } geometry)
			{
				continue;
			}

			drawingContext.DrawGeometry(
				TextHighlight.Brush,
				null,
				geometry);
		}
	}

	/// <summary>
	/// Finds the occurrences of the selected text in the visible lines, the selection itself excluded.
	/// </summary>
	internal IEnumerable<SimpleSegment> FindOccurrences(TextView textView)
	{
		Selection selection = _textArea.Selection;

		// Only a piece of one line is looked for.
		if (textView.Document is not { } document
			|| !textView.VisualLinesValid
			|| textView.VisualLines.Count == 0
			|| selection is RectangleSelection
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

		int start = textView.VisualLines[0].FirstDocumentLine.Offset;

		string text = document.GetText(start, textView.VisualLines[^1].LastDocumentLine.EndOffset - start);

		int index = 0;

		while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) >= 0)
		{
			int offset = start + index;

			if (offset != selected.Offset && (!isWord || HasWordBoundaries(document, offset, pattern.Length)))
			{
				yield return new SimpleSegment(offset, pattern.Length);
			}

			index += pattern.Length;
		}
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="TextArea.SelectionChanged" /> handler, which repaints the occurrences.
	/// </summary>
	private void TextArea_SelectionChanged(object? sender, EventArgs e)
	{
		_textArea.TextView.InvalidateLayer(Layer);
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
