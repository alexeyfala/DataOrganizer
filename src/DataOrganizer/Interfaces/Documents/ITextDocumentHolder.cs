using AvaloniaEdit.Document;

namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Holds the document of the editor that keeps a text.
/// </summary>
public interface ITextDocumentHolder
{
	#region Properties
	/// <summary>
	/// The document of the editor.
	/// </summary>
	TextDocument Document { get; }
	#endregion
}
