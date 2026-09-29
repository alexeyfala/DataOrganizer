using AvaloniaEdit.Folding;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Text;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="ILineFoldingStrategy" /> that folds a group of line comments and a block comment of several lines into a
/// box with their first line, as Visual Studio folds comments.
/// </summary>
internal sealed class CommentFoldingStrategy : ILineFoldingStrategy
{
	#region Data
	/// <summary>
	/// Characters of the indentation of a line.
	/// </summary>
	private const string Blanks = " \t";

	/// <summary>
	/// The fewest lines of a group of line comments that folds.
	/// </summary>
	private const int MinGroupLineCount = 2;

	/// <summary>
	/// Rules of the language for the comments and the markers.
	/// </summary>
	private readonly SyntaxFoldingRules _rules;
	#endregion

	#region Constructors
	public CommentFoldingStrategy(SyntaxFoldingRules rules)
	{
		_rules = rules;
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public NewFolding[] CreateNewFoldings(FoldingText text)
	{
		List<NewFolding> blocks = [];

		// The group under way: its first and last lines, the number of its lines and the blanks before each of them.
		int first = 0;

		int last = 0;

		int lineCount = 0;

		ReadOnlySpan<char> groupIndentation = [];

		for (int number = 1; number <= text.LineCount; number++)
		{
			ReadOnlySpan<char> line = text.GetLine(number);

			int indentLength = line.IndexOfAnyExcept(Blanks);

			// Blank lines may stand inside a group, as in Visual Studio.
			if (indentLength < 0)
			{
				continue;
			}

			ReadOnlySpan<char> content = line[indentLength..];

			// A block comment comes before a line comment, as the token of one may start the other, as in Lua.
			if (_rules.BlockCommentStart is { } start
				&& _rules.BlockCommentEnd is { } end
				&& content.StartsWith(start))
			{
				AddGroup(text, blocks, first, last, lineCount);

				lineCount = 0;

				// A comment that stays open holds the rest of the text, where no other comment can start.
				if (FindBlockCommentEnd(text, number, indentLength + start.Length, end) is not { } close)
				{
					break;
				}

				// A comment of one line has nothing to fold.
				if (close.Line > number)
				{
					blocks.Add(new NewFolding(text.GetLineStart(number) + indentLength, close.End)
					{
						Name = CreateName(content)
					});
				}

				number = close.Line;

				continue;
			}

			if (IsGroupLine(line, content))
			{
				ReadOnlySpan<char> indentation = line[..indentLength];

				// A line of another indentation starts a group of its own.
				if (lineCount == 0 || !indentation.SequenceEqual(groupIndentation))
				{
					AddGroup(text, blocks, first, last, lineCount);

					first = number;

					lineCount = 0;

					groupIndentation = indentation;
				}

				last = number;

				lineCount++;

				continue;
			}

			// Any other line ends the group under way.
			AddGroup(text, blocks, first, last, lineCount);

			lineCount = 0;
		}

		// The blank lines after the last line of a group stay out of it.
		AddGroup(text, blocks, first, last, lineCount);

		return [.. blocks];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds the block of a group that holds enough lines, from the token of its first line to the end of its last line.
	/// </summary>
	private static void AddGroup(FoldingText text, List<NewFolding> blocks, int first, int last, int lineCount)
	{
		if (lineCount < MinGroupLineCount)
		{
			return;
		}

		ReadOnlySpan<char> line = text.GetLine(first);

		int indentLength = line.IndexOfAnyExcept(Blanks);

		blocks.Add(new NewFolding(text.GetLineStart(first) + indentLength, text.GetLineEnd(last))
		{
			Name = CreateName(line[indentLength..])
		});
	}

	/// <summary>
	/// Returns the name that the box of a folded comment shows: its first line with dots after it, as in Visual Studio.
	/// </summary>
	private static string CreateName(ReadOnlySpan<char> content) => $"{content.TrimEnd()} {Glyphs.ThreeDots}";

	/// <summary>
	/// Returns the line where a block comment that opens before an index of a line closes, with the offset after its end
	/// token; <c>null</c> when the comment stays open to the end of the text.
	/// </summary>
	private static (int Line, int End)? FindBlockCommentEnd(FoldingText text, int number, int index, string end)
	{
		for (int line = number; line <= text.LineCount; line++)
		{
			// The end is looked for after the start, as the two may be one token.
			int from = line == number ? index : 0;

			int found = text
				.GetLine(line)[from..]
				.IndexOf(end, StringComparison.Ordinal);

			if (found >= 0)
			{
				return (line, text.GetLineStart(line) + from + found + end.Length);
			}
		}

		return null;
	}

	/// <summary>
	/// Returns <c>true</c> when a line is a line comment that may join a group, but not a marker, as it heads a block of
	/// its own.
	/// </summary>
	private bool IsGroupLine(ReadOnlySpan<char> line, ReadOnlySpan<char> content)
	{
		return _rules.LineComment?.IsMatch(content) == true
			&& _rules.StartMarker?.IsMatch(line) != true
			&& _rules.EndMarker?.IsMatch(line) != true;
	}
	#endregion
}
