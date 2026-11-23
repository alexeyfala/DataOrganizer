using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using DataOrganizer.Documents.TextProcessing.Helpers;
using DataOrganizer.UI.Extensions;
using DataOrganizer.UI.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.UI.Controls.Text;

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
	/// <summary>
	/// Initializes a new instance of the <see cref="DocumentFoldingAdapter" /> class.
	/// </summary>
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
			.CreateNewFoldings(document.Text, [])
			.Select(static x => x.ToNewFolding())];
	}
	#endregion
}
