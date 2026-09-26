using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CommunityToolkit.Mvvm.Messaging;
using DataOrganizer.Helpers.Text;
using DataOrganizer.Messages.Documents;
using Material.Icons;
using System;

namespace DataOrganizer.Controls;

/// <summary>
/// Margin with the bookmarks of the lines, where a click sets or removes the bookmark of a line.
/// </summary>
internal sealed class BookmarkMargin : AbstractMargin, IRecipient<BookmarksChangedMessage>
{
	#region Data
	/// <summary>
	/// Side of the square the Material icons are drawn in.
	/// </summary>
	private const double IconBoxSize = 24.0;

	/// <summary>
	/// Opacity of the icon that shows where a click puts a bookmark.
	/// </summary>
	private const double PreviewOpacity = 0.4;

	/// <summary>
	/// Width of the margin as a share of the font size.
	/// </summary>
	private const double WidthRatio = 1.2;

	/// <summary>
	/// Outline of the icon of a bookmark.
	/// </summary>
	private static readonly Geometry Icon = Geometry.Parse(MaterialIconDataProvider.GetData(MaterialIconKind.Bookmark));

	/// <summary>
	/// Bookmarks the margin shows and changes.
	/// </summary>
	private readonly LineBookmarks _bookmarks;

	/// <summary>
	/// Height of the pointer over the margin.
	/// </summary>
	private double? _pointerY;
	#endregion

	#region Constructors
	public BookmarkMargin(LineBookmarks bookmarks)
	{
		_bookmarks = bookmarks;

		// The margin widens with the zoom, like the line numbers.
		this
			.GetObservable(TextElement.FontSizeProperty)
			.Subscribe(_ => InvalidateMeasure());
	}
	#endregion

	#region Methods
	/// <summary>
	/// Repaints the margin when its bookmarks change.
	/// </summary>
	public void Receive(BookmarksChangedMessage message)
	{
		// Every open editor sends the message about its own bookmarks.
		if (message.Bookmarks != _bookmarks)
		{
			return;
		}

		InvalidateVisual();
	}

	/// <inheritdoc />
	public override void Render(DrawingContext context)
	{
		// A background of its own lets the margin take clicks.
		context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

		if (TextView is not { VisualLinesValid: true } textView)
		{
			return;
		}

		IBrush brush = TextHighlight.FindBookmarkBrush(this);

		VisualLine? pointedLine = FindPointedLine();

		foreach (VisualLine visualLine in textView.VisualLines)
		{
			bool isBookmarked = _bookmarks.Contains(visualLine.FirstDocumentLine.LineNumber);

			// The icon under the pointer shows where a click puts a bookmark.
			if (!isBookmarked && visualLine != pointedLine)
			{
				continue;
			}

			DrawIcon(
				context,
				textView,
				visualLine,
				brush,
				isBookmarked ? 1.0 : PreviewOpacity);
		}
	}

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		return new Size(
			width: GetValue(TextElement.FontSizeProperty) * WidthRatio,
			height: 0.0);
	}

	/// <inheritdoc />
	protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
	{
		base.OnAttachedToVisualTree(e);

		// The keys and the menu change the bookmarks too.
		WeakReferenceMessenger
			.Default
			.RegisterAll(this);
	}

	/// <inheritdoc />
	protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
	{
		WeakReferenceMessenger
			.Default
			.UnregisterAll(this);

		base.OnDetachedFromVisualTree(e);
	}

	/// <inheritdoc />
	protected override void OnPointerExited(PointerEventArgs e)
	{
		base.OnPointerExited(e);

		SetPointerY(null);
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerEventArgs e)
	{
		base.OnPointerMoved(e);

		SetPointerY(e.GetPosition(this).Y);
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (!e.GetCurrentPoint(this)
			.Properties
			.IsLeftButtonPressed)
		{
			return;
		}

		// The click belongs to the margin, so the caret and the selection stay where they are.
		e.Handled = true;

		if (TextView is not { } textView
			|| textView.GetVisualLineFromVisualTop(e.GetPosition(this).Y + textView.VerticalOffset) is not { } visualLine)
		{
			return;
		}

		_bookmarks.Toggle(visualLine.FirstDocumentLine.LineNumber);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Draws the icon of a bookmark level with the text of the first row of a visual line.
	/// </summary>
	private void DrawIcon(
		DrawingContext context,
		TextView textView,
		VisualLine visualLine,
		IBrush brush,
		double opacity)
	{
		TextLine row = visualLine.TextLines[0];

		// The engine sets the text in the middle of a taller row, so the icon takes the middle of the text, like the line number.
		double middle = visualLine.GetTextLineVisualYPosition(row, VisualYPosition.TextTop) + (row.Height / 2.0) - textView.VerticalOffset;

		double size = Math.Min(Bounds.Width, textView.DefaultLineHeight);

		double scale = size / IconBoxSize;

		double left = (Bounds.Width - size) / 2.0;

		double top = middle - (size / 2.0);

		using (context.PushOpacity(opacity))
		using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(left, top)))
		{
			context.DrawGeometry(
				brush,
				null,
				Icon);
		}
	}

	/// <summary>
	/// Returns the visual line under the pointer.
	/// </summary>
	private VisualLine? FindPointedLine()
	{
		return _pointerY is { } y && TextView is { VisualLinesValid: true } textView
			? textView.GetVisualLineFromVisualTop(y + textView.VerticalOffset)
			: null;
	}

	/// <summary>
	/// Keeps the height of the pointer and repaints the margin when the pointer comes to another line.
	/// </summary>
	private void SetPointerY(double? y)
	{
		VisualLine? pointedLine = FindPointedLine();

		_pointerY = y;

		if (FindPointedLine() == pointedLine)
		{
			return;
		}

		InvalidateVisual();
	}
	#endregion
}
