using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using System.Collections.Generic;

namespace DataOrganizer.Interfaces.Text;

/// <summary>
/// Way of finding the blocks of a document that fold.
/// </summary>
internal interface IFoldingStrategy
{
	#region Methods
	/// <summary>
	/// Returns the blocks of a document sorted by their start, with the offset where the reading of a broken text
	/// stopped; -1 when the whole text was read.
	/// </summary>
	IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset);
	#endregion
}
