using AvaloniaEdit.Document;
using DataOrganizer.Documents.Editor.Interfaces;
using DataOrganizer.UI.Interfaces.Documents;

namespace DataOrganizer.UI.UnitTests.TestHelpers;

/// <summary>
/// Access to the documents of the editor behind the texts of view models, for tests that type into them.
/// </summary>
internal static class DocumentTextExtensions
{
	#region Methods
	/// <summary>
	/// Returns the document of the editor that keeps <paramref name="text" />.
	/// </summary>
	public static TextDocument AsTextDocument(this IDocumentText text) => ((ITextDocumentHolder)text).Document;
	#endregion
}
