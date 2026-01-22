using AvaloniaEdit;
using System.Drawing;

namespace DataOrganizer.DTO;

/// <summary>
/// The properties of file in built-in editor.
/// </summary>
public readonly struct FileProperties
{
	#region Properties
	/// <summary>
	/// The caret position.
	/// </summary>
	public required TextViewPosition CaretPosition { get; init; }

	/// <summary>
	/// Font size.
	/// </summary>
	public required double FontSize { get; init; }

	/// <summary>
	/// Indicates the need to wrap words.
	/// </summary>
	public required bool IsWordWrap { get; init; }

	/// <summary>
	/// The offset of scrolling position.
	/// </summary>
	public required Point ScrollOffset { get; init; }

	/// <summary>
	/// The length of selected text.
	/// </summary>
	public required int SelectionLength { get; init; }

	/// <summary>
	/// The start of selected text.
	/// </summary>
	public required int SelectionStart { get; init; }
	#endregion
}
