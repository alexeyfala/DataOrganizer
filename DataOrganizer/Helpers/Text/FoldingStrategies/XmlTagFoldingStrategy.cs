using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using DataOrganizer.Interfaces.Text;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="IFoldingStrategy" /> of XML, which folds the elements and the comments of several lines by their tags,
/// with the strategy of the engine.
/// </summary>
internal sealed class XmlTagFoldingStrategy : IFoldingStrategy
{
	#region Data
	/// <summary>
	/// Strategy of the engine, which reads the text as XML up to its first error.
	/// </summary>
	private readonly XmlFoldingStrategy _strategy = new();
	#endregion

	#region Methods
	/// <inheritdoc />
	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
	{
		return _strategy.CreateNewFoldings(document, out firstErrorOffset);
	}
	#endregion
}
