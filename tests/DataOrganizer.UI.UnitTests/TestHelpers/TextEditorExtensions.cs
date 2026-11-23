using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using System.Linq;

namespace DataOrganizer.UI.UnitTests.TestHelpers;

/// <summary>
/// States of a text editor that a test reads.
/// </summary>
internal static class TextEditorExtensions
{
	#region Methods
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
