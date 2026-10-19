namespace DataOrganizer.Interfaces.Documents;

/// <summary>
/// Creates empty texts of documents.
/// </summary>
public interface IDocumentTextFactory
{
	#region Methods
	/// <summary>
	/// Creates an empty text with no undo history.
	/// </summary>
	IDocumentText Create();
	#endregion
}
