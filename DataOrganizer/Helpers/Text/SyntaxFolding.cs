using Avalonia.Input;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Helpers.Text.FoldingStrategies;
using DataOrganizer.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text;

/// <summary>
/// Folding of the blocks of a text area by the rules of a language, with the margin of their markers.
/// </summary>
internal sealed class SyntaxFolding : IDisposable
{
	#region Properties
	/// <summary>
	/// Document whose blocks fold; the folding serves this document only.
	/// </summary>
	public TextDocument Document { get; }

	/// <summary>
	/// Language whose rules find the blocks.
	/// </summary>
	public string Language { get; }
	#endregion

	#region Data
	/// <summary>
	/// Blocks of the document, with the margin of their markers in the text area.
	/// </summary>
	private readonly FoldingManager _manager;

	/// <summary>
	/// Way of finding the blocks of the language.
	/// </summary>
	private readonly IndentFoldingStrategy _strategy;
	#endregion

	#region Constructors
	public SyntaxFolding(TextArea textArea, string language, SyntaxFoldingRules rules)
	{
		// The manager takes the document the text area has now and keeps it.
		Document = textArea.Document;

		Language = language;

		_strategy = new(rules, textArea.Options.IndentationSize);

		_manager = FoldingManager.Install(textArea);

		// A cursor of its own, as the margin would inherit the I-beam the text area takes on a click.
		textArea
			.LeftMargins
			.OfType<FoldingMargin>()
			.Single(x => x.FoldingManager == _manager)
			.Cursor = new Cursor(StandardCursorType.Arrow);

		// The blocks come with the text.
		Update();
	}
	#endregion

	#region Methods
	/// <summary>
	/// Removes the blocks and their margin from the text area, and the folded text comes back into view.
	/// </summary>
	public void Dispose() => FoldingManager.Uninstall(_manager);

	/// <summary>
	/// Finds the blocks anew with a pass over all lines; a folded block that is found again stays folded.
	/// </summary>
	public void Update()
	{
		IEnumerable<NewFolding> foldings = _strategy.CreateNewFoldings(Document, out int firstErrorOffset);

		_manager.UpdateFoldings(foldings, firstErrorOffset);
	}
	#endregion
}
