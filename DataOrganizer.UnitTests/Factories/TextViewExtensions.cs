using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit.Rendering;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Points of the characters of a text view, where a test clicks or hovers.
/// </summary>
internal static class TextViewExtensions
{
	#region Methods
	/// <summary>
	/// Returns the middle of the element that starts at an offset of the text view, in the coordinates of its window.
	/// </summary>
	public static Point GetColumnCenter(this TextView textView, int offset)
	{
		VisualLine visualLine = textView.GetVisualLine(textView.Document.GetLineByOffset(offset).LineNumber)!;

		int column = visualLine.GetVisualColumn(offset - visualLine.StartOffset);

		Point left = visualLine.GetVisualPosition(column, VisualYPosition.LineMiddle);

		Point right = visualLine.GetVisualPosition(column + 1, VisualYPosition.LineMiddle);

		// The visual positions stand in the coordinates of the document, which the view scrolls.
		Point center = new Point((left.X + right.X) / 2.0, left.Y) - textView.ScrollOffset;

		return textView.TranslatePoint(center, TopLevel.GetTopLevel(textView)!) ?? default;
	}
	#endregion
}
