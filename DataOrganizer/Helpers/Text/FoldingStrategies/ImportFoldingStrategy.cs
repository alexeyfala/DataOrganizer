using AvaloniaEdit.Folding;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Text;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="ILineFoldingStrategy" /> that folds a run of import statements behind the first word of its first line, as
/// Visual Studio folds the using directives.
/// </summary>
internal sealed class ImportFoldingStrategy : ILineFoldingStrategy
{
	#region Data
	/// <summary>
	/// Characters of the indentation of a line.
	/// </summary>
	private const string Blanks = " \t";

	/// <summary>
	/// Brackets that close what the opening ones of a statement open.
	/// </summary>
	private const string ClosingBrackets = ")]}";

	/// <summary>
	/// The fewest statements of a run that folds.
	/// </summary>
	private const int MinStatementCount = 2;

	/// <summary>
	/// Brackets that carry a statement over to the next line while they stay open.
	/// </summary>
	private const string OpeningBrackets = "([{";

	/// <summary>
	/// Marks that open and close a quoted text on one line.
	/// </summary>
	private const string Quotes = "\"'`";

	/// <summary>
	/// Characters that end the first word of a line: the blanks and the marks that may follow a keyword at once.
	/// </summary>
	private static readonly SearchValues<char> WordEnds = SearchValues.Create(" \t({[<\"';:");

	/// <summary>
	/// Rules of the language for the imports, the comments, the directives and the markers.
	/// </summary>
	private readonly SyntaxFoldingRules _rules;
	#endregion

	#region Constructors
	public ImportFoldingStrategy(SyntaxFoldingRules rules)
	{
		_rules = rules;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public NewFolding[] CreateNewFoldings(FoldingText text)
	{
		if (_rules.ImportLine is not { } importLine)
		{
			return [];
		}

		List<NewFolding> blocks = [];

		// The run under way: its first line, the last line of its last statement and the number of its statements.
		int first = 0;

		int last = 0;

		int statementCount = 0;

		for (int number = 1; number <= text.LineCount; number++)
		{
			ReadOnlySpan<char> line = text.GetLine(number);

			if (importLine.IsMatch(line))
			{
				// Brackets that stay open to the end of the text make no statement, and no import can follow them.
				if (FindStatementEnd(text, number) is not { } end)
				{
					break;
				}

				if (statementCount == 0)
				{
					first = number;
				}

				statementCount++;

				last = end;

				number = end;

				continue;
			}

			// A block comment is passed over whole, so that the imports in it do not count, and it may stand in a run.
			if (FindCommentEnd(text, number) is { } commentEnd)
			{
				number = commentEnd;

				continue;
			}

			// Any other line but a gap ends the run under way.
			if (statementCount > 0 && !IsGap(line))
			{
				AddRun(text, blocks, first, last, statementCount);

				statementCount = 0;
			}
		}

		// The gaps after the last statement stay out of the run.
		AddRun(text, blocks, first, last, statementCount);

		return [.. blocks];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds the block of a run that holds enough statements, from the first word of its first line and the blanks after
	/// it to the end of its last line.
	/// </summary>
	private static void AddRun(FoldingText text, List<NewFolding> blocks, int first, int last, int statementCount)
	{
		if (statementCount < MinStatementCount)
		{
			return;
		}

		ReadOnlySpan<char> line = text.GetLine(first);

		// The keyword of the imports stays in view before the box, as in Visual Studio.
		int wordStart = line.IndexOfAnyExcept(Blanks);

		int wordLength = line[wordStart..].IndexOfAny(WordEnds);

		int wordEnd = wordLength < 0 ? line.Length : wordStart + wordLength;

		int blankCount = line[wordEnd..].IndexOfAnyExcept(Blanks);

		int column = blankCount < 0 ? line.Length : wordEnd + blankCount;

		blocks.Add(new NewFolding(text.GetLineStart(first) + column, text.GetLineEnd(last)));
	}

	/// <summary>
	/// Returns the index of the mark that closes the quote at an index of a line; -1 when the quote stays open.
	/// </summary>
	private static int FindQuoteEnd(ReadOnlySpan<char> line, int start)
	{
		char quote = line[start];

		for (int index = start + 1; index < line.Length; index++)
		{
			// A backslash keeps the next character inside the quote.
			if (line[index] == '\\')
			{
				index++;
			}
			else if (line[index] == quote)
			{
				return index;
			}
		}

		return -1;
	}

	/// <summary>
	/// Returns the opening brackets of a line less its closing ones, leaving out the quoted text and the line comment.
	/// </summary>
	private int CountBrackets(ReadOnlySpan<char> line)
	{
		int depth = 0;

		for (int index = 0; index < line.Length; index++)
		{
			char character = line[index];

			// A line comment takes the rest of the line, even where its token is a quote, as in Visual Basic.
			if (_rules.LineComment?.IsMatch(line[index..]) == true)
			{
				break;
			}

			if (Quotes.Contains(character))
			{
				int end = FindQuoteEnd(line, index);

				// A quote that stays open takes the rest of the line.
				if (end < 0)
				{
					break;
				}

				index = end;
			}
			else if (OpeningBrackets.Contains(character))
			{
				depth++;
			}
			else if (ClosingBrackets.Contains(character))
			{
				depth--;
			}
		}

		return depth;
	}

	/// <summary>
	/// Returns the number of the line where a block comment that opens a line closes, the last line when it stays open;
	/// <c>null</c> when the line opens no block comment.
	/// </summary>
	private int? FindCommentEnd(FoldingText text, int number)
	{
		if (_rules is not { BlockCommentStart: { } start, BlockCommentEnd: { } end })
		{
			return null;
		}

		ReadOnlySpan<char> content = text
			.GetLine(number)
			.TrimStart(Blanks);

		if (!content.StartsWith(start))
		{
			return null;
		}

		// The end is looked for after the start, as the two may be one token.
		if (content[start.Length..].Contains(end, StringComparison.Ordinal))
		{
			return number;
		}

		for (int next = number + 1; next <= text.LineCount; next++)
		{
			if (text
				.GetLine(next)
				.Contains(end, StringComparison.Ordinal))
			{
				return next;
			}
		}

		// A comment that stays open runs to the end of the text.
		return text.LineCount;
	}

	/// <summary>
	/// Returns the number of the last line of the statement that starts on a line, where the brackets it opens close;
	/// <c>null</c> when they stay open to the end of the text.
	/// </summary>
	private int? FindStatementEnd(FoldingText text, int number)
	{
		int depth = 0;

		for (int line = number; line <= text.LineCount; line++)
		{
			depth += CountBrackets(text.GetLine(line));

			if (depth <= 0)
			{
				return line;
			}
		}

		return null;
	}

	/// <summary>
	/// Returns <c>true</c> when a line may stand between the statements of a run: a blank line, a line comment or
	/// a directive, but not a marker, as it heads a block of its own.
	/// </summary>
	private bool IsGap(ReadOnlySpan<char> line)
	{
		if (_rules.StartMarker?.IsMatch(line) == true || _rules.EndMarker?.IsMatch(line) == true)
		{
			return false;
		}

		ReadOnlySpan<char> content = line.TrimStart(Blanks);

		return content.IsEmpty
			|| _rules.LineComment?.IsMatch(content) == true
			|| _rules.DirectiveLine?.IsMatch(line) == true;
	}
	#endregion
}
