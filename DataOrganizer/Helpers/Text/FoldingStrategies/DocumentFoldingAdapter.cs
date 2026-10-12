using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using DataOrganizer.Interfaces.Text;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="IFoldingStrategy" /> that finds the blocks of a document in its text with a
/// <see cref="CompositeFoldingStrategy" />.
/// </summary>
internal sealed class DocumentFoldingAdapter : IFoldingStrategy
{
	#region Data
	/// <inheritdoc cref="CompositeFoldingStrategy" />
	private readonly CompositeFoldingStrategy _strategy;
	#endregion

	#region Constructors
	public DocumentFoldingAdapter(CompositeFoldingStrategy strategy)
	{
		_strategy = strategy;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
	{
		// The ways read any text.
		firstErrorOffset = -1;

		return [.. _strategy
			.CreateNewFoldings(document.Text)
			.Select(static x => new NewFolding(x.StartOffset, x.EndOffset)
			{
				Name = x.Name
			})];
	}
	#endregion
}
