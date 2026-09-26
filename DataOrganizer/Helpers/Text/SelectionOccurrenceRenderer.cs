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
		if (textView.Document is not { } document
			|| !textView.VisualLinesValid
			|| textView.VisualLines.Count == 0)
		{
			return [];
		}

		int start = textView.VisualLines[0].FirstDocumentLine.Offset;

		int end = textView.VisualLines[^1].LastDocumentLine.EndOffset;

		int? selectedOffset = _textArea.Selection.SurroundingSegment?.Offset;

		// The selected piece keeps the pure color of the selection.
		return SelectionOccurrenceFinder
			.Find(document, _textArea.Selection, start, end)
			.Where(x => x.Offset != selectedOffset);
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
}
