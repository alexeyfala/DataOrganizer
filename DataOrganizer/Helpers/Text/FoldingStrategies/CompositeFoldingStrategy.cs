using DataOrganizer.Interfaces.Text;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// Joins the blocks of several ways over one reading of the text, with one block per line and no block that crosses
/// another.
/// </summary>
internal sealed class CompositeFoldingStrategy
{
	#region Data
	/// <summary>
	/// Ways of finding the blocks, in the order that decides between two blocks with the same bounds.
	/// </summary>
	private readonly ILineFoldingStrategy[] _strategies;
	#endregion

	#region Constructors
	public CompositeFoldingStrategy(params ILineFoldingStrategy[] strategies)
	{
		_strategies = strategies;
	}
	#endregion

	#region Methods
	/// <summary>
	/// Returns the blocks of a text sorted by their start.
	/// </summary>
	public List<TextFolding> CreateNewFoldings(string text)
	{
		using FoldingText lines = new(text);

		// Of the blocks that start together the outer one comes first, and the sort keeps the order of the ways for the rest.
		IEnumerable<TextFolding> blocks = _strategies
			.SelectMany(x => x.CreateNewFoldings(lines))
			.OrderBy(static x => x.StartOffset)
			.ThenByDescending(static x => x.EndOffset);

		return SelectNested(lines, blocks);
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the blocks, sorted by their start, that start first on their lines and nest in each other.
	/// </summary>
	private static List<TextFolding> SelectNested(FoldingText text, IEnumerable<TextFolding> blocks)
	{
		List<TextFolding> selected = [];

		// The ends of the selected blocks that hold the start of the next one, the innermost on top.
		Stack<int> openEnds = [];

		int lastLine = 0;

		foreach (TextFolding block in blocks)
		{
			int line = text.GetLineNumber(block.StartOffset);

			// The margin marks the first block of a line alone, and the chord toggles that block too.
			if (line == lastLine)
			{
				continue;
			}

			// A block that starts where another ends does not cross it.
			while (openEnds.Count > 0 && openEnds.Peek() <= block.StartOffset)
			{
				openEnds.Pop();
			}

			// A block that starts inside an open one and ends after it would cross it.
			if (openEnds.Count > 0 && block.EndOffset > openEnds.Peek())
			{
				continue;
			}

			selected.Add(block);

			openEnds.Push(block.EndOffset);

			lastLine = line;
		}

		return selected;
	}
	#endregion
}
