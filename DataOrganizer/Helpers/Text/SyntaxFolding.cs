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
	/// <c>True</c> when a block is folded.
	/// </summary>
	public bool HasFoldedBlocks => _manager.AllFoldings.Any(static x => x.IsFolded);

	/// <summary>
	/// <c>True</c> when a block is unfolded.
	/// </summary>
	public bool HasUnfoldedBlocks => _manager.AllFoldings.Any(static x => !x.IsFolded);

	/// <summary>
	/// Language whose rules find the blocks.
	/// </summary>
	public string Language { get; }
	#endregion

	#region Data
	/// <summary>
	/// Language of Markdown, whose headings fold their sections.
	/// </summary>
	private const string MarkdownLanguage = "markdown";

	/// <summary>
	/// Language of XML, whose tags fold their elements.
	/// </summary>
	private const string XmlLanguage = "xml";

	/// <summary>
	/// Language of XSL, which is XML as well.
	/// </summary>
	private const string XslLanguage = "xsl";

	/// <summary>
	/// Blocks of the document, with the margin of their markers in the text area.
	/// </summary>
	private readonly FoldingManager _manager;

	/// <summary>
	/// Way of finding the blocks of the language.
	/// </summary>
	private readonly IFoldingStrategy _strategy;
	#endregion

	#region Constructors
	public SyntaxFolding(TextArea textArea, string language, SyntaxFoldingRules rules)
	{
		// The manager takes the document the text area has now and keeps it.
		Document = textArea.Document;

		Language = language;

		_strategy = CreateStrategy(language, rules, textArea.Options.IndentationSize);

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
	/// Returns the innermost block in view whose lines hold a line; <c>null</c> when no block holds it.
	/// </summary>
	public FoldingSection? FindBlock(int line)
	{
		DocumentLine documentLine = Document.GetLineByNumber(line);

		FoldingSection? found = null;

		// The end of the last folded block, before which a block is hidden in it.
		int hiddenEnd = -1;

		// The blocks come in the order of their starts, so the last one to hold the line is the innermost.
		foreach (FoldingSection block in _manager.AllFoldings)
		{
			if (block.StartOffset > documentLine.EndOffset)
			{
				break;
			}

			if (block.StartOffset < hiddenEnd)
			{
				continue;
			}

			if (block.IsFolded)
			{
				hiddenEnd = block.EndOffset;
			}

			if (block.EndOffset >= documentLine.Offset)
			{
				found = block;
			}
		}

		return found;
	}

	/// <summary>
	/// Returns the outermost folded block that hides an offset; <c>null</c> when the offset is in view.
	/// </summary>
	public FoldingSection? FindFoldedBlock(int offset)
	{
		// The ends of a folded block stay in view, before and after the box of its hidden text.
		return _manager
			.GetFoldingsContaining(offset)
			.Where(x => x.IsFolded && x.StartOffset < offset && offset < x.EndOffset)
			.MinBy(static x => x.StartOffset);
	}

	/// <summary>
	/// Folds every block.
	/// </summary>
	public void FoldAll() => SetAllFolded(isFolded: true);

	/// <summary>
	/// Unfolds every block.
	/// </summary>
	public void UnfoldAll() => SetAllFolded(isFolded: false);

	/// <summary>
	/// Finds the blocks anew with a pass over all lines; a folded block that is found again stays folded.
	/// </summary>
	public void Update()
	{
		IEnumerable<NewFolding> foldings = _strategy.CreateNewFoldings(Document, out int firstErrorOffset);

		_manager.UpdateFoldings(foldings, firstErrorOffset);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the way of finding the blocks of a language: by tags for XML, by headings for Markdown and by indentation
	/// for the others.
	/// </summary>
	private static IFoldingStrategy CreateStrategy(string language, SyntaxFoldingRules rules, int tabSize) => language switch
	{
		MarkdownLanguage => new MarkdownFoldingStrategy(rules, tabSize),
		XmlLanguage or XslLanguage => new XmlTagFoldingStrategy(),
		_ => new IndentFoldingStrategy(rules, tabSize)
	};

	/// <summary>
	/// Folds or unfolds every block.
	/// </summary>
	private void SetAllFolded(bool isFolded)
	{
		foreach (FoldingSection block in _manager.AllFoldings)
		{
			block.IsFolded = isFolded;
		}
	}
	#endregion
}
