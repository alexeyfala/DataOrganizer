using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace DataOrganizer.Controls.Text;

/// <summary>
/// Selects the whole run of spaces and tabs that a double click in a <see cref="TextEditorBase" /> lands on, where the engine
/// selects the symbol before the run.
/// </summary>
internal static class WhitespaceRunSelection
{
	#region Methods
	/// <summary>
	/// Starts to take the double clicks on spaces and tabs of a text area from the engine.
	/// </summary>
	public static void Attach(TextArea textArea)
	{
		// The engine would select its word again when the button is released, so the press never reaches it.
		textArea.AddHandler(
			InputElement.PointerPressedEvent,
			TextArea_PointerPressed,
			RoutingStrategies.Tunnel);
	}
	#endregion

	#region Event Handlers
	/// <summary>
	/// <see cref="InputElement.PointerPressedEvent" /> handler of the text area, before the engine sees the press.
	/// </summary>
	private static void TextArea_PointerPressed(object? sender, PointerPressedEventArgs e)
	{
		if (sender is not TextArea textArea
			|| e.ClickCount != 2
			|| e.KeyModifiers != KeyModifiers.None
			|| e.Pointer.Type != PointerType.Mouse
			|| !e.GetCurrentPoint(textArea).Properties.IsLeftButtonPressed
			|| FindWhitespaceRun(textArea.TextView, e.GetPosition(textArea.TextView)) is not { } run)
		{
			return;
		}

		textArea.Selection = Selection.Create(textArea, run.Start, run.End);

		e.Handled = true;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the run of spaces and tabs around the character under a point of a text view, or <c>null</c> when that
	/// character is neither.
	/// </summary>
	private static (int Start, int End)? FindWhitespaceRun(TextView textView, Point point)
	{
		if (textView.Document is not { } document
			|| textView.GetPositionFloor(point + textView.ScrollOffset) is not { } position)
		{
			return null;
		}

		int offset = document.GetOffset(position.Location);

		if (offset >= document.TextLength || document.GetCharAt(offset) is not (' ' or '\t'))
		{
			return null;
		}

		int start = TextUtilities
			.GetWhitespaceBefore(document, offset)
			.Offset;

		int end = TextUtilities
			.GetWhitespaceAfter(document, offset)
			.EndOffset;

		return (start, end);
	}
	#endregion
}
