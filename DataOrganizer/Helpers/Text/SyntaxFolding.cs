using Avalonia;
using Avalonia.Input;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
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
	/// The most lines of a hidden text that its tip shows.
	/// </summary>
	private const int MaxTipLineCount = 20;

	/// <summary>
	/// The most characters of a line of a hidden text that its tip shows.
	/// </summary>
	private const int MaxTipLineLength = 120;

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

	/// <summary>
	/// View of the text area, where the folded blocks show as boxes.
	/// </summary>
	private readonly TextView _textView;
	#endregion

	#region Constructors
	public SyntaxFolding(TextArea textArea, string language, SyntaxFoldingRules rules)
	{
		// The manager takes the document the text area has now and keeps it.
		Document = textArea.Document;

		Language = language;

		_strategy = CreateStrategy(language, rules, textArea.Options.IndentationSize);

		_textView = textArea.TextView;

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
	/// Returns the first lines of the text that the box of a folded block hides at a point of the view; <c>null</c> when
	/// no box is there.
	/// </summary>
	public string? FindHiddenText(Point point)
	{
		// The visual lines stand in the coordinates of the document, which the view scrolls.
		Point position = point + _textView.ScrollOffset;

		if (_textView.GetVisualLineFromVisualTop(position.Y) is not { } visualLine)
		{
			return null;
		}

		int column = visualLine.GetVisualColumnFloor(position);

		// The element under the point, found the way the view finds the one that a click goes to.
		if (visualLine
			.Elements
			.FirstOrDefault(x => x.VisualColumn + x.VisualLength > column) is not { } element)
		{
			return null;
		}

		int start = visualLine.StartOffset + element.RelativeTextOffset;

		// The box is the only element that starts where a folded block does, as it takes the whole text of the block.
		if (!_manager
			.GetFoldingsAt(start)
			.Any(static x => x.IsFolded))
		{
			return null;
		}

		return GetHiddenText(Document, start, start + element.DocumentLength);
	}

	/// <summary>
	/// Folds every block.
	/// </summary>
	public void FoldAll() => SetAllFolded(isFolded: true);

	/// <summary>
	/// Returns the offsets where the folded blocks start, or the unfolded ones, in the order of the text.
	/// </summary>
	public int[] GetBlockStarts(bool isFolded)
	{
		return [.. _manager
			.AllFoldings
			.Where(x => x.IsFolded == isFolded)
			.Select(static x => x.StartOffset)];
	}

	/// <summary>
	/// Folds or unfolds the blocks that start at the offsets and turns the other blocks the other way; an offset where no
	/// block starts is skipped.
	/// </summary>
	public void SetBlocksFolded(IEnumerable<int> starts, bool isFolded)
	{
		// Only the blocks found in the text change, so an offset cannot hide a text that no block holds.
		HashSet<int> listed = [.. starts];

		foreach (FoldingSection block in _manager.AllFoldings)
		{
			block.IsFolded = listed.Contains(block.StartOffset) ? isFolded : !isFolded;
		}
	}

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
	/// Returns the way of finding the blocks of a language: by tags for XML, by headings and comments for Markdown and by
	/// indentation, imports and comments for the others.
	/// </summary>
	private static IFoldingStrategy CreateStrategy(string language, SyntaxFoldingRules rules, int tabSize) => language switch
	{
		MarkdownLanguage => new CompositeFoldingStrategy(
			new MarkdownFoldingStrategy(rules, tabSize),
			new CommentFoldingStrategy(rules)),
		XmlLanguage or XslLanguage => new XmlTagFoldingStrategy(),
		_ => new CompositeFoldingStrategy(
			new IndentFoldingStrategy(rules, tabSize),
			new ImportFoldingStrategy(rules),
			new CommentFoldingStrategy(rules))
	};

	/// <summary>
	/// Returns the first lines of a hidden text, each cut to a length, with an ellipsis for what is left out.
	/// </summary>
	private static string GetHiddenText(TextDocument document, int start, int end)
	{
		DocumentLine firstLine = document.GetLineByOffset(start);

		// A block that starts at the end of a line hides no text of that line, only its line break.
		if (start == firstLine.EndOffset && firstLine.NextLine is { } nextLine)
		{
			firstLine = nextLine;
		}

		List<string> lines = [];

		for (DocumentLine? line = firstLine; line is not null && line.Offset < end; line = line.NextLine)
		{
			if (lines.Count == MaxTipLineCount)
			{
				lines.Add(Glyphs.HorizontalEllipsis);

				break;
			}

			int from = Math.Max(line.Offset, start);

			int length = Math.Min(line.EndOffset, end) - from;

			lines.Add(length > MaxTipLineLength
				? document.GetText(from, MaxTipLineLength) + Glyphs.HorizontalEllipsis
				: document.GetText(from, length));
		}

		return string.Join('\n', lines);
	}

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
