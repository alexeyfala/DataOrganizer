using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// Paints in the visible lines of the editor the occurrences of the text selected in its source,
/// its own text area or another view of the same document.
/// </summary>
internal sealed class SelectionOccurrenceRenderer : IBackgroundRenderer
{
	#region Data
	/// <summary>
	/// Text area whose visible lines are painted.
	/// </summary>
	private readonly TextArea _textArea;

	/// <summary>
	/// Text area whose selected text is looked for.
	/// </summary>
	private TextArea _source;
	#endregion

	#region Constructors
	public SelectionOccurrenceRenderer(TextArea textArea, IObservable<TextArea> source)
	{
		_textArea = textArea;

		// Until the source comes, the own selection is looked for.
		_source = textArea;

		// The engine repaints the view only when its own selection changes in or above the visible lines.
		source.Subscribe(Source_SelectionChanged);
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
	/// Finds the occurrences of the text selected in the source in the visible lines, the piece selected in this view
	/// excluded.
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

		// The piece selected in this view keeps the pure color of the selection.
		return SelectionOccurrenceFinder
			.Find(document, _source.Selection, start, end)
			.Where(x => x.Offset != selectedOffset);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// Handler of the source and of each change of its selection, which repaints the occurrences.
	/// </summary>
	private void Source_SelectionChanged(TextArea value)
	{
		_source = value;

		_textArea.TextView.InvalidateLayer(Layer);
	}
	#endregion
}
