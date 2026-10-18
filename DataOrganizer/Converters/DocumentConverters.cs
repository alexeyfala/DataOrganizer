using Avalonia.Data.Converters;
using AvaloniaEdit.Document;
using DataOrganizer.Interfaces.Documents;

namespace DataOrganizer.Converters;

/// <summary>
/// Stateless one-way value converters of the texts of documents, referenced via x:Static.
/// </summary>
internal static class DocumentConverters
{
	#region Properties
	/// <summary>
	/// Document of the editor that keeps a text.
	/// </summary>
	public static FuncValueConverter<ITextDocumentHolder?, TextDocument?> ToTextDocument { get; } =
		new(static x => x?.Document);
	#endregion
}
