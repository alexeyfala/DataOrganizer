using DataOrganizer.Services.Documents;
using DataOrganizer.ViewModels;

namespace DataOrganizer.UnitTests.Factories;

/// <summary>
/// Factory methods that build editors of a text.
/// </summary>
public static class DocumentEditorFactory
{
	#region Methods
	/// <summary>
	/// Creates an editor of the text of bytes, read in the encoding chosen by its web name or in the one found from them.
	/// </summary>
	public static DocumentEditorViewModel CreateEditor(byte[] contents, string? encoding = null)
	{
		DocumentEditorViewModel editor = new(new AvaloniaDocumentText());

		editor.ReplaceText(editor.Codec.Read(contents, encoding)!);

		return editor;
	}
	#endregion
}
