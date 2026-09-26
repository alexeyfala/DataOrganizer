using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using DataOrganizer.Converters;
using DataOrganizer.Extensions;
using DataOrganizer.Helpers;
using DataOrganizer.Helpers.Text;
using Shared.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;

namespace DataOrganizer.Controls;

/// <summary>
/// Margin that marks the occurrences of the selected text and the caret line along the whole document,
/// in the scale of the scroll bar.
/// </summary>
internal sealed class ScrollMarkMargin : AbstractMargin
{
	#region Data
	/// <summary>
	/// Height of the line that stands for the caret.
	/// </summary>
	private const double CaretLineHeight = 2.0;

	/// <summary>
	/// Height of the mark under the pointer.
	/// </summary>
	private const double HoveredMarkHeight = 6.0;

	/// <summary>
	/// Height of a mark.
	/// </summary>
	private const double MarkHeight = 4.0;

	/// <summary>
	/// Distance from a mark within which the pointer is taken to be over it.
	/// </summary>
	private const double MarkHitTolerance = 2.0;

	/// <summary>
	/// Gap between a mark and the sides of the margin.
	/// </summary>
	private const double MarkInset = 2.0;

	/// <summary>
	/// Characters kept before the first occurrence when a tip cuts a long line at its start.
	/// </summary>
	private const int TipLeadLength = 20;

	/// <summary>
	/// The most characters of a line that a tip takes.
	/// </summary>
	private const int TipTextLength = 200;

	/// <summary>
	/// The widest line of text in a tip.
	/// </summary>
	private const double TipTextWidth = 600.0;

	/// <summary>
	/// Pause after the last change of the selection or the text before the marks are found again.
	/// </summary>
	private static readonly TimeSpan MarksDelay = TimeSpan.FromSeconds(0.2);

	/// <summary>
	/// Editor whose document the margin marks.
	/// </summary>
	private readonly TextEditorBase _editor;

	/// <summary>
	/// Marks drawn last: the row of pixels of each and the first line it stands for.
	/// </summary>
	private readonly List<(double Row, int Line)> _marks = [];

	/// <summary>
	/// Row of pixels of the mark under the pointer.
	/// </summary>
	private double? _hoveredRow;

	/// <summary>
	/// Lines with the occurrences of the selected text, in the order of the document.
	/// </summary>
	private int[] _markLines = [];
	#endregion

	#region Constructors
	public ScrollMarkMargin(TextEditorBase editor)
	{
		_editor = editor;

		TextView = editor.TextArea.TextView;

		editor.TextArea.Caret.PositionChanged += Caret_PositionChanged;

		// A pass over the whole document follows a run of changes rather than every step of a mouse selection.
		Observable
			.FromEventPattern<EventHandler, EventArgs>(
				x => editor.TextArea.SelectionChanged += x,
				x => editor.TextArea.SelectionChanged -= x)
			.Merge(Observable.FromEventPattern<EventHandler, EventArgs>(
				x => editor.TextChanged += x,
				x => editor.TextChanged -= x))
			.SetDelay(MarksDelay)
			.Subscribe(_ => UpdateMarks());
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="Caret.PositionChanged" /> event handler.
	/// </summary>
	private void Caret_PositionChanged(object? sender, EventArgs e) => InvalidateVisual();
	#endregion

	#region Methods
	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		// A background of its own lets the margin take clicks.
		context.FillRectangle(
			Brushes.Transparent,
			new Rect(Bounds.Size));

		_marks.Clear();

		if (TextView is not { Document: { } document, DocumentHeight: > 0.0 } textView)
		{
			return;
		}

		(double top, double height) = GetTrackRange();

		foreach (int line in _markLines)
		{
			// The lines found before an edit may be gone until the next pass.
			if (line > document.LineCount)
			{
				break;
			}

			double row = GetRow(textView, line, top, height);

			// The marks of one row of pixels are drawn once.
			if (_marks.Count > 0 && _marks[^1].Row == row)
			{
				continue;
			}

			_marks.Add((row, line));
		}

		foreach ((double row, _) in _marks)
		{
			if (row == _hoveredRow)
			{
				continue;
			}

			DrawMark(context, row, MarkInset, MarkHeight);
		}

		// The mark under the pointer grows to the whole width and comes above its neighbours.
		if (_hoveredRow is { } hoveredRow && _marks.Exists(x => x.Row == hoveredRow))
		{
			DrawMark(context, hoveredRow, 0.0, HoveredMarkHeight);
		}

		double caretRow = GetRow(textView, TextArea.Caret.Line, top, height);

		context.FillRectangle(
			TextArea.Foreground ?? Brushes.Gray,
			new Rect(
				x: 0.0,
				y: caretRow - (CaretLineHeight / 2.0),
				width: Bounds.Width,
				height: CaretLineHeight));
	}

	/// <summary>
	/// Finds the lines with the occurrences of the selected text in the whole document.
	/// </summary>
	internal void UpdateMarks()
	{
		_markLines = TextView?.Document is { } document
			? [.. SelectionOccurrenceFinder
				.Find(document, TextArea.Selection, 0, document.TextLength)
				.Select(x => document.GetLineByOffset(x.Offset).LineNumber)
				.Distinct()]
			: [];

		InvalidateVisual();
	}

	/// <inheritdoc />
	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);

		SetHoveredRow(null);
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		SetHoveredRow(FindMarkRow(e.GetPosition(this).Y));
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
			|| FindScrollViewer() is not { } scrollViewer
			|| TextView is not { DocumentHeight: > 0.0 } textView)
		{
			return;
		}

		e.Handled = true;

		(double top, double height) = GetTrackRange();

		double fraction = Math.Clamp((e.GetPosition(this).Y - top) / height, 0.0, 1.0);

		// The place comes to the middle of the view, as in the map mode of Visual Studio, and the caret stays.
		double offset = (fraction * textView.DocumentHeight) - (scrollViewer.Viewport.Height / 2.0);

		scrollViewer.Offset = scrollViewer.Offset.WithY(Math.Clamp(
			offset,
			0.0,
			Math.Max(0.0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height)));
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the row of pixels that stands for the middle of a line on the scroll range.
	/// </summary>
	private static double GetRow(TextView textView, int line, double top, double height)
	{
		double lineTop = textView.GetVisualTopByDocumentLine(line);

		double lineBottom = line < textView.Document.LineCount
			? textView.GetVisualTopByDocumentLine(line + 1)
			: textView.DocumentHeight;

		return Math.Round(top + ((lineTop + lineBottom) / 2.0 / textView.DocumentHeight * height));
	}

	/// <summary>
	/// Builds the runs of the text of a line with the occurrences of the selected text on the highlight;
	/// a long line is cut so that the first occurrence stays in view.
	/// </summary>
	private InlineCollection CreateLineInlines(TextDocument document, DocumentLine line)
	{
		SimpleSegment[] occurrences = [.. SelectionOccurrenceFinder.Find(
			document,
			TextArea.Selection,
			line.Offset,
			line.EndOffset)];

		string text = document.GetText(line);

		// The indentation tells nothing about the occurrence.
		int start = line.Offset + (text.Length - text.TrimStart().Length);

		InlineCollection inlines = [];

		if (occurrences.Length > 0 && (occurrences[0].Offset - start) > TipLeadLength)
		{
			start = occurrences[0].Offset - TipLeadLength;

			inlines.Add(new Run(Glyphs.HorizontalEllipsis));
		}

		int end = Math.Min(line.EndOffset, start + TipTextLength);

		int position = start;

		foreach (SimpleSegment occurrence in occurrences)
		{
			if (occurrence.Offset >= end)
			{
				break;
			}

			int from = Math.Max(position, occurrence.Offset);

			int to = Math.Min(end, occurrence.EndOffset);

			if (from > position)
			{
				inlines.Add(new Run(document.GetText(position, from - position)));
			}

			inlines.Add(new Run(document.GetText(from, to - from))
			{
				Background = TextHighlight.Brush
			});

			position = to;
		}

		if (end > position)
		{
			inlines.Add(new Run(document.GetText(position, end - position)));
		}

		return inlines;
	}

	/// <summary>
	/// Builds the tip of the mark on a row of pixels: the number of the first line it stands for and the text of that line.
	/// </summary>
	private StackPanel? CreateTip(double? row)
	{
		if (row is null || TextView?.Document is not { } document)
		{
			return null;
		}

		int line = _marks.Find(x => x.Row == row).Line;

		// The lines found before an edit may be gone until the next pass.
		if (line < 1 || line > document.LineCount)
		{
			return null;
		}

		StackPanel tip = new()
		{
			Orientation = Orientation.Vertical,
			Children =
			{
				// The same caption as in the status bar.
				new TextBlock
				{
					Text = AppConverters.NumberCaption.Convert(
						line,
						typeof(string),
						Strings.LineFormat,
						CultureInfo.CurrentCulture) as string
				}
			}
		};

		// The text of an encrypted file stays in the text area; the line number tells nothing of it.
		if (_editor.IsSensitive)
		{
			return tip;
		}

		tip.Children.Add(new TextBlock
		{
			FontFamily = TextArea.FontFamily,
			Inlines = CreateLineInlines(document, document.GetLineByNumber(line)),
			MaxWidth = TipTextWidth,
			TextTrimming = TextTrimming.CharacterEllipsis
		});

		return tip;
	}

	/// <summary>
	/// Draws a mark on a row of pixels, set in from the sides of the margin.
	/// </summary>
	private void DrawMark(DrawingContext context, double row, double inset, double height)
	{
		context.FillRectangle(
			TextHighlight.MarkBrush,
			new Rect(
				x: inset,
				y: row - (height / 2.0),
				width: Bounds.Width - (2.0 * inset),
				height: height));
	}

	/// <summary>
	/// Returns the row of the mark within reach of the pointer, the nearest one where marks stand close.
	/// </summary>
	private double? FindMarkRow(double y)
	{
		return _marks
			.Select(static x => x.Row)
			.Where(x => Math.Abs(x - y) <= ((MarkHeight / 2.0) + MarkHitTolerance))
			.OrderBy(x => Math.Abs(x - y))
			.Select(static x => (double?)x)
			.FirstOrDefault();
	}

	/// <summary>
	/// Finds the scroll viewer around the text area.
	/// </summary>
	private ScrollViewer? FindScrollViewer() => TextArea?.FindAncestorOfType<ScrollViewer>();

	/// <summary>
	/// Returns the top and the height of the track of the vertical scroll bar, or of the whole margin without it.
	/// </summary>
	private (double Top, double Height) GetTrackRange()
	{
		Track? track = FindScrollViewer()?
			.GetTemplateDescendants()
			.OfType<ScrollBar>()
			.FirstOrDefault(static x => x.Orientation == Orientation.Vertical)?
			.GetTemplateDescendants()
			.OfType<Track>()
			.FirstOrDefault();

		if (track is { IsEffectivelyVisible: true, Bounds.Height: > 0.0 }
			&& track.TranslatePoint(default, this) is { } origin)
		{
			return (origin.Y, track.Bounds.Height);
		}

		return (0.0, Bounds.Height);
	}

	/// <summary>
	/// Repaints the margin when another mark comes under the pointer.
	/// </summary>
	private void SetHoveredRow(double? row)
	{
		if (row == _hoveredRow)
		{
			return;
		}

		_hoveredRow = row;

		// The tip is built for the mark under the pointer only, and the tooltip service shows it after its delay.
		ToolTip.SetTip(this, CreateTip(row));

		InvalidateVisual();
	}
	#endregion
}
