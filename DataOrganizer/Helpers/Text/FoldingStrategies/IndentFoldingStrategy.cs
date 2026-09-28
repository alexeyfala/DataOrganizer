using AvaloniaEdit.Document;
using AvaloniaEdit.Folding;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="IFoldingStrategy" /> that folds by indentation and by the block markers of a language, as VS Code does.
/// </summary>
internal sealed class IndentFoldingStrategy : IFoldingStrategy
{
	#region Data
	/// <summary>
	/// Closing brackets, each at the index of its opening one in <see cref="OpeningBrackets" />.
	/// </summary>
	private const string ClosingBrackets = ")]}";

	/// <summary>
	/// Characters of a line that only closes: the closing brackets and the ends of a statement and of an item.
	/// </summary>
	private const string ClosingCharacters = ")]};,";

	/// <summary>
	/// Indentation of an end marker in the list of open blocks, lower than that of any line.
	/// </summary>
	private const int EndMarkerIndent = -2;

	/// <summary>
	/// Indentation of a blank line and of the bottom of the list of open blocks.
	/// </summary>
	private const int NoIndent = -1;

	/// <summary>
	/// Opening brackets, each at the index of its closing one in <see cref="ClosingBrackets" />.
	/// </summary>
	private const string OpeningBrackets = "([{";

	/// <summary>
	/// Rules of the language for the markers and the blank lines.
	/// </summary>
	private readonly SyntaxFoldingRules _rules;

	/// <summary>
	/// Columns from one tab stop to the next.
	/// </summary>
	private readonly int _tabSize;
	#endregion

	#region Constructors
	public IndentFoldingStrategy(SyntaxFoldingRules rules, int tabSize)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tabSize);

		_rules = rules;

		_tabSize = tabSize;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
	{
		// Indentation reads any text.
		firstErrorOffset = -1;

		using FoldingText text = new(document);

		return CreateNewFoldings(text);
	}

	/// <summary>
	/// Returns the blocks of a text read at once, sorted by their start.
	/// </summary>
	internal NewFolding[] CreateNewFoldings(FoldingText text)
	{
		List<(int Start, int End)> blocks = FindBlocks(text);

		// A language of the off-side rule sets its blocks by indentation alone.
		if (!_rules.IsOffSide)
		{
			AttachBrackets(text, blocks);
		}

		// The first line of a block stays in view, and the rest of the block folds into it.
		return [.. blocks.Select(x => new NewFolding(text.GetLineEnd(x.Start), text.GetLineEnd(x.End)))];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Returns the indentation of a line in columns, where a tab reaches the next tab stop; <see cref="NoIndent" /> for
	/// a blank line.
	/// </summary>
	private static int GetIndent(ReadOnlySpan<char> text, int tabSize)
	{
		int indent = 0;

		foreach (char character in text)
		{
			switch (character)
			{
				case ' ':
					indent++;
					break;

				case '\t':
					indent += tabSize - (indent % tabSize);
					break;

				default:
					return indent;
			}
		}

		return NoIndent;
	}

	/// <summary>
	/// Returns <c>true</c> when the trimmed text of a line starts with a closing bracket and goes on with nothing
	/// but closing characters.
	/// </summary>
	private static bool IsClosingLine(ReadOnlySpan<char> content, char bracket)
	{
		return content.Length > 0
			&& content[0] == bracket
			&& !content[1..].ContainsAnyExcept(ClosingCharacters);
	}

	/// <summary>
	/// Returns <c>true</c> when the trimmed text of a line finishes what came before it: a statement, an item or a block.
	/// </summary>
	private static bool IsFinishedLine(ReadOnlySpan<char> content) => content[^1] is ',' or ';' || !content.ContainsAnyExcept(ClosingCharacters);

	/// <summary>
	/// Starts the block of a lone opening bracket on the line above it and takes the line of the closing bracket into
	/// the block, so that a folded block shows its head alone, as in Visual Studio.
	/// </summary>
	private void AttachBrackets(FoldingText text, List<(int Start, int End)> blocks)
	{
		for (int i = 0; i < blocks.Count; i++)
		{
			(int start, int end) = blocks[i];

			// The line of the opening bracket, whose indentation the closing one shares.
			ReadOnlySpan<char> opening = text.GetLine(start);

			ReadOnlySpan<char> content = opening.Trim();

			int indent = GetIndent(opening, _tabSize);

			if (content is [var bracket]
				&& OpeningBrackets.Contains(bracket)
				&& FindHead(text, start, indent) is { } head)
			{
				start = head;
			}

			int bracketIndex = OpeningBrackets.IndexOf(content[^1]);

			int next = end + 1;

			if (bracketIndex >= 0 && next <= text.LineCount)
			{
				ReadOnlySpan<char> closing = text.GetLine(next);

				if (GetIndent(closing, _tabSize) == indent
					&& IsClosingLine(closing.Trim(), ClosingBrackets[bracketIndex]))
				{
					end = next;
				}
			}

			blocks[i] = (start, end);
		}
	}

	/// <summary>
	/// Returns the blocks by indentation and by markers as the numbers of their first and last lines, sorted by the first.
	/// </summary>
	private List<(int Start, int End)> FindBlocks(FoldingText text)
	{
		List<(int Start, int End)> blocks = [];

		int lineCount = text.LineCount;

		// The lines are read upwards, as VS Code reads them; the bottom of the list lies under the last line.
		List<OpenFoldingBlock> openBlocks =
		[
			new OpenFoldingBlock(Indent: NoIndent, EndAbove: lineCount + 1, Line: lineCount + 1)
		];

		for (int number = lineCount; number > 0; number--)
		{
			ReadOnlySpan<char> line = text.GetLine(number);

			int indent = GetIndent(line, _tabSize);

			if (indent == NoIndent)
			{
				// Under the off-side rule a blank line belongs to the block below it.
				if (_rules.IsOffSide)
				{
					openBlocks[^1] = openBlocks[^1] with
					{
						EndAbove = number
					};
				}

				continue;
			}

			if (_rules.StartMarker?.IsMatch(line) == true)
			{
				int index = openBlocks.FindLastIndex(static x => x.Indent == EndMarkerIndent);

				// A start without an end below it is an ordinary line.
				if (index >= 0)
				{
					blocks.Add((number, openBlocks[index].Line));

					openBlocks.RemoveRange(index + 1, openBlocks.Count - index - 1);

					// The blocks above take the start marker for an ordinary line of its indentation.
					openBlocks[index] = new OpenFoldingBlock(Indent: indent, EndAbove: number, Line: number);

					continue;
				}
			}
			else if (_rules.EndMarker?.IsMatch(line) == true)
			{
				openBlocks.Add(new OpenFoldingBlock(Indent: EndMarkerIndent, EndAbove: number, Line: number));

				continue;
			}

			OpenFoldingBlock previous = openBlocks[^1];

			if (previous.Indent > indent)
			{
				// The deeper lines below make up the block of this line.
				do
				{
					openBlocks.RemoveAt(openBlocks.Count - 1);

					previous = openBlocks[^1];
				}
				while (previous.Indent > indent);

				int end = previous.EndAbove - 1;

				// A block needs a line to fold under its first one.
				if (end > number)
				{
					blocks.Add((number, end));
				}
			}

			if (previous.Indent == indent)
			{
				openBlocks[^1] = previous with
				{
					EndAbove = number
				};
			}
			else
			{
				openBlocks.Add(new OpenFoldingBlock(Indent: indent, EndAbove: number, Line: number));
			}
		}

		// Read upwards, the blocks came from the last to the first.
		blocks.Reverse();

		return blocks;
	}

	/// <summary>
	/// Returns the line that a lone opening bracket below it continues, as the head of a method or of a statement;
	/// <c>null</c> when the bracket stands on its own.
	/// </summary>
	private int? FindHead(FoldingText text, int line, int indent)
	{
		for (int number = line - 1; number > 0; number--)
		{
			ReadOnlySpan<char> head = text.GetLine(number);

			int headIndent = GetIndent(head, _tabSize);

			// Blank lines may stand between the head and its bracket.
			if (headIndent == NoIndent)
			{
				continue;
			}

			// The head goes on into the bracket, while a marker heads a block of its own.
			if (headIndent != indent
				|| IsFinishedLine(head.Trim())
				|| _rules.StartMarker?.IsMatch(head) == true
				|| _rules.EndMarker?.IsMatch(head) == true)
			{
				return null;
			}

			return number;
		}

		return null;
	}
	#endregion
}
