using AvaloniaEdit.Folding;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Text;
using System;
using System.Collections.Generic;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="ILineFoldingStrategy" /> that folds a group of line comments and a block comment of several lines into a
/// box with their first line, and a documentation comment into a box with its summary, as Visual Studio folds comments.
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

		// The group under way: its first and last lines, the number of its lines, the blanks before each of them and whether
		// they are lines of a documentation comment.
		int first = 0;

		int last = 0;

		int lineCount = 0;

		ReadOnlySpan<char> groupIndentation = [];

		bool isDocGroup = false;

		for (int number = 1; number <= text.LineCount; number++)
		{
			ReadOnlySpan<char> line = text.GetLine(number);

			int indentLength = line.IndexOfAnyExcept(Blanks);

			// Blank lines may stand inside a group, as in Visual Studio, while a documentation comment ends at one, as in C#.
			if (indentLength < 0)
			{
				if (isDocGroup)
				{
					AddGroup(text, blocks, first, last, lineCount, isDocGroup);

					lineCount = 0;
				}

				continue;
			}

			ReadOnlySpan<char> content = line[indentLength..];

			// A block comment comes before a line comment, as the token of one may start the other, as in Lua.
			if (_rules.BlockCommentStart is { } start
				&& _rules.BlockCommentEnd is { } end
				&& content.StartsWith(start))
			{
				AddGroup(text, blocks, first, last, lineCount, isDocGroup);

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

			// A line of a documentation comment comes before a line comment, as its token starts with the other one.
			bool isDocLine = IsDocLine(content);

			if (isDocLine || IsGroupLine(line, content))
			{
				ReadOnlySpan<char> indentation = line[..indentLength];

				// A line of another kind or of another indentation starts a group of its own.
				if (lineCount == 0 || isDocLine != isDocGroup || !indentation.SequenceEqual(groupIndentation))
				{
					AddGroup(text, blocks, first, last, lineCount, isDocGroup);

					first = number;

					lineCount = 0;

					groupIndentation = indentation;

					isDocGroup = isDocLine;
				}

				last = number;

				lineCount++;

				continue;
			}

			// Any other line ends the group under way.
			AddGroup(text, blocks, first, last, lineCount, isDocGroup);

			lineCount = 0;
		}

		// The blank lines after the last line of a group stay out of it.
		AddGroup(text, blocks, first, last, lineCount, isDocGroup);

		return [.. blocks];
	}
	#endregion

	#region Helpers
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
	/// Adds the block of a group that holds enough lines, from the token of its first line to the end of its last line.
	/// </summary>
	private void AddGroup(FoldingText text, List<NewFolding> blocks, int first, int last, int lineCount, bool isDoc)
	{
		if (lineCount < MinGroupLineCount)
		{
			return;
		}

		ReadOnlySpan<char> line = text.GetLine(first);

		int indentLength = line.IndexOfAnyExcept(Blanks);

		int start = text.GetLineStart(first) + indentLength;

		int end = text.GetLineEnd(last);

		// A documentation comment shows its summary, as in Visual Studio, and its first line when it has none.
		string? summary = isDoc && _rules.DocComment is { } token
			? DocCommentBanner.Create(token, text.GetText(start, end))
			: null;

		blocks.Add(new NewFolding(start, end)
		{
			Name = summary ?? CreateName(line[indentLength..])
		});
	}

	/// <summary>
	/// Returns <c>true</c> when a text starts with the token of a documentation comment, but not with a longer run of its
	/// last mark, which makes a plain comment, as in C#.
	/// </summary>
	private bool IsDocLine(ReadOnlySpan<char> content)
	{
		return _rules.DocComment is { } token
			&& content.StartsWith(token, StringComparison.Ordinal)
			&& !content[token.Length..].StartsWith(token[^1]);
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
