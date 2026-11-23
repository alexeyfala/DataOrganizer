using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using DataOrganizer.UI.Controls.Text;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.TestHelpers;

/// <summary>
/// Parts and states of a text editor that a test reads.
/// </summary>
internal static class TextEditorExtensions
{
	#region Data
	/// <summary>
	/// Name of the scroll viewer in the template of the editor.
	/// </summary>
	private const string ScrollViewerName = "PART_ScrollViewer";
	#endregion

	#region Methods
	/// <summary>
	/// Returns the scroll viewer of the editor.
	/// </summary>
	public static ScrollViewer GetScrollViewer(this TextEditorBase editor)
	{
		return editor
			.GetVisualDescendants()
			.OfType<ScrollViewer>()
			.First(static x => x.Name == ScrollViewerName);
	}

	/// <summary>
	/// Returns the numbers of the lines that start the first lines of the view of an editor, from its top.
	/// </summary>
	public static int[] GetShownLines(this TextEditor editor, int count)
	{
		return [.. editor
			.TextArea
			.TextView
			.VisualLines
			.Take(count)
			.Select(static x => x.FirstDocumentLine.LineNumber)];
	}

	/// <summary>
	/// <c>True</c> when the editor has the syntax highlighting.
	/// </summary>
	public static bool HasHighlighting(this TextEditor editor)
	{
		return editor
			.TextArea
			.TextView
			.LineTransformers
			.OfType<TextMateColoringTransformer>()
			.Any();
	}
	#endregion
}
