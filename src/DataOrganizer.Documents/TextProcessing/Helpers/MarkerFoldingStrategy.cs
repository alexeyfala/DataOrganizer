using DataOrganizer.Documents.TextProcessing.Dto;
using DataOrganizer.Documents.TextProcessing.Interfaces;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Documents.TextProcessing.Helpers;

/// <summary>
/// <see cref="ILineFoldingStrategy" /> that folds only the blocks between the start and the end markers of a language,
/// such as regions.
/// </summary>
public sealed class MarkerFoldingStrategy : ILineFoldingStrategy
{
	#region Data
	/// <summary>
	/// Rules of the language for the markers.
	/// </summary>
	private readonly SyntaxFoldingRules _rules;
	#endregion

	#region Constructors
	public MarkerFoldingStrategy(SyntaxFoldingRules rules)
	{
		_rules = rules;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public TextFolding[] CreateNewFoldings(FoldingText text)
	{
		if (_rules.StartMarker is not { } startMarker || _rules.EndMarker is not { } endMarker)
		{
			return [];
		}

		List<TextFolding> blocks = [];

		// The lines of the start markers still open, the innermost on top.
		Stack<int> openStarts = [];

		for (int number = 1; number <= text.LineCount; number++)
		{
			ReadOnlySpan<char> line = text.GetLine(number);

			if (startMarker.IsMatch(line))
			{
				openStarts.Push(number);
			}
			// An end without a start above it is an ordinary line, as is a start without an end below it.
			else if (endMarker.IsMatch(line) && openStarts.TryPop(out int start))
			{
				// The line of the start marker stays in view, and the rest of the block folds into it.
				blocks.Add(new TextFolding
				{
					EndOffset = text.GetLineEnd(number),
					StartOffset = text.GetLineEnd(start)
				});
			}
		}

		return [.. blocks];
	}
	#endregion
}
