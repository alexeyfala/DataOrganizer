using AvaloniaEdit.Folding;
using DataOrganizer.Dto.Documents;
using DataOrganizer.Interfaces.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// <see cref="ILineFoldingStrategy" /> of Markdown, which folds the section of each heading, as VS Code does, and the
/// other blocks by indentation and by markers.
/// </summary>
internal sealed class MarkdownFoldingStrategy : ILineFoldingStrategy
{
	#region Data
	/// <summary>
	/// Characters of a fence around a block of code, three or more of one kind.
	/// </summary>
	private const string FenceCharacters = "`~";

	/// <summary>
	/// Line that opens and closes the front matter at the start of a text.
	/// </summary>
	private const string FrontMatterDelimiter = "---";

	/// <summary>
	/// Line that may close the front matter too, as the end of a YAML document.
	/// </summary>
	private const string FrontMatterEnd = "...";

	/// <summary>
	/// The deepest level of a heading.
	/// </summary>
	private const int MaxHeadingLevel = 6;

	/// <summary>
	/// The most spaces before a heading, a fence or an underline, as four make a line of code.
	/// </summary>
	private const int MaxMarkIndent = 3;

	/// <summary>
	/// The fewest characters of a fence.
	/// </summary>
	private const int MinFenceLength = 3;

	/// <summary>
	/// Strategy of the blocks by indentation and by markers, such as the items of nested lists.
	/// </summary>
	private readonly IndentFoldingStrategy _indentStrategy;
	#endregion

	#region Constructors
	public MarkdownFoldingStrategy(SyntaxFoldingRules rules, int tabSize)
	{
		_indentStrategy = new IndentFoldingStrategy(rules, tabSize);
	}
	#endregion

	#region Methods
	/// <inheritdoc />
	public NewFolding[] CreateNewFoldings(FoldingText text)
	{
		Dictionary<int, NewFolding> foldings = [];

		foreach (NewFolding section in FindSections(text))
		{
			foldings[section.StartOffset] = section;
		}

		// A section holds a block that starts on the line of its heading, so the line goes to the section.
		foreach (NewFolding block in _indentStrategy.CreateNewFoldings(text))
		{
			foldings.TryAdd(block.StartOffset, block);
		}

		return [.. foldings.Values.OrderBy(static x => x.StartOffset)];
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Adds the section of a heading down to a line, leaving a blank line before the next heading in view, as VS Code does.
	/// </summary>
	private static void AddSection(FoldingText text, List<NewFolding> sections, int heading, int end)
	{
		if (end > heading && text.GetLine(end).IsWhiteSpace())
		{
			end--;
		}

		// A heading needs a line to fold under it.
		if (end > heading)
		{
			sections.Add(new NewFolding(text.GetLineEnd(heading), text.GetLineEnd(end)));
		}
	}

	/// <summary>
	/// Returns the number of times a character repeats at the start of a text.
	/// </summary>
	private static int CountLeading(ReadOnlySpan<char> text, char character)
	{
		int count = text.IndexOfAnyExcept(character);

		return count < 0 ? text.Length : count;
	}

	/// <summary>
	/// Returns the headings of a text, each with the number of its line and its level, in the order of the text.
	/// </summary>
	private static List<(int Line, int Level)> FindHeadings(FoldingText text)
	{
		List<(int Line, int Level)> headings = [];

		// The character of the fence of the block of code that the line is in; none outside such a block.
		char fence = '\0';

		int fenceLength = 0;

		for (int number = SkipFrontMatter(text); number <= text.LineCount; number++)
		{
			if (!TryGetMark(text.GetLine(number), out ReadOnlySpan<char> mark))
			{
				continue;
			}

			// A block of code holds no headings up to the fence that closes it.
			if (fence != '\0')
			{
				if (CountLeading(mark, fence) >= fenceLength
					&& mark
						.TrimStart(fence)
						.IsWhiteSpace())
				{
					fence = '\0';
				}

				continue;
			}

			if (IsFence(mark))
			{
				fence = mark[0];

				fenceLength = CountLeading(mark, fence);

				continue;
			}

			if (GetHeadingLevel(mark) is { } level)
			{
				headings.Add((number, level));

				continue;
			}

			// An underline makes a heading of the line of text above it.
			if (number > 1
				&& GetUnderlineLevel(mark) is { } underlineLevel
				&& IsParagraphLine(text.GetLine(number - 1)))
			{
				headings.Add((number - 1, underlineLevel));
			}
		}

		return headings;
	}

	/// <summary>
	/// Returns the sections of the headings of a text, each down to the next heading of its level or of a higher one.
	/// </summary>
	private static List<NewFolding> FindSections(FoldingText text)
	{
		List<NewFolding> sections = [];

		// The headings whose sections are still open; a heading closes those of its level and of the deeper ones.
		List<(int Line, int Level)> openHeadings = [];

		foreach ((int line, int level) in FindHeadings(text))
		{
			while (openHeadings.Count > 0 && openHeadings[^1].Level >= level)
			{
				AddSection(text, sections, openHeadings[^1].Line, line - 1);

				openHeadings.RemoveAt(openHeadings.Count - 1);
			}

			openHeadings.Add((line, level));
		}

		foreach ((int line, _) in openHeadings)
		{
			AddSection(text, sections, line, text.LineCount);
		}

		return sections;
	}

	/// <summary>
	/// Returns the level of a heading that a mark starts; <c>null</c> when the mark is no heading.
	/// </summary>
	private static int? GetHeadingLevel(ReadOnlySpan<char> mark)
	{
		int level = CountLeading(mark, '#');

		// One to six signs and then a space or nothing, so that a #tag stays text.
		if (level is 0 or > MaxHeadingLevel
			|| (level < mark.Length && mark[level] is not (' ' or '\t')))
		{
			return null;
		}

		return level;
	}

	/// <summary>
	/// Returns the level of the heading that an underline of equal signs or of dashes makes; <c>null</c> when the mark
	/// is no underline.
	/// </summary>
	private static int? GetUnderlineLevel(ReadOnlySpan<char> mark)
	{
		ReadOnlySpan<char> content = mark.TrimEnd();

		if (content.IsEmpty)
		{
			return null;
		}

		if (!content.ContainsAnyExcept('='))
		{
			return 1;
		}

		return content.ContainsAnyExcept('-') ? null : 2;
	}

	/// <summary>
	/// Returns <c>true</c> when a mark is a fence of a block of code: three or more backticks or tildes.
	/// </summary>
	private static bool IsFence(ReadOnlySpan<char> mark)
	{
		return mark is [var character, ..]
			&& FenceCharacters.Contains(character)
			&& CountLeading(mark, character) >= MinFenceLength;
	}

	/// <summary>
	/// Returns <c>true</c> when a mark starts an item of a list, with a bullet or a number before a space.
	/// </summary>
	private static bool IsListItem(ReadOnlySpan<char> mark)
	{
		ReadOnlySpan<char> rest;

		if (mark is ['-' or '*' or '+', ..])
		{
			rest = mark[1..];
		}
		else
		{
			int digits = mark.IndexOfAnyExceptInRange('0', '9');

			if (digits is < 1 or > 9 || mark[digits] is not ('.' or ')'))
			{
				return false;
			}

			rest = mark[(digits + 1)..];
		}

		return rest.IsEmpty || rest[0] is ' ' or '\t';
	}

	/// <summary>
	/// Returns <c>true</c> when a line is text that an underline below it makes a heading of.
	/// </summary>
	private static bool IsParagraphLine(ReadOnlySpan<char> line)
	{
		if (!TryGetMark(line, out ReadOnlySpan<char> mark) || mark.IsWhiteSpace())
		{
			return false;
		}

		// Under a heading, a fence, a quote or an item of a list an underline stays a rule or text.
		return GetHeadingLevel(mark) is null
			&& GetUnderlineLevel(mark) is null
			&& !IsFence(mark)
			&& mark[0] != '>'
			&& !IsListItem(mark);
	}

	/// <summary>
	/// Returns the number of the first line after the front matter at the start of a text; the first line without
	/// front matter.
	/// </summary>
	private static int SkipFrontMatter(FoldingText text)
	{
		if (text.GetLine(1).TrimEnd() is not FrontMatterDelimiter)
		{
			return 1;
		}

		for (int number = 2; number <= text.LineCount; number++)
		{
			if (text.GetLine(number).TrimEnd() is FrontMatterDelimiter or FrontMatterEnd)
			{
				return number + 1;
			}
		}

		// Without its closing line it is no front matter.
		return 1;
	}

	/// <summary>
	/// Returns <c>true</c> when a line may hold a mark, with at most three spaces before it; the mark is the rest of the line.
	/// </summary>
	private static bool TryGetMark(ReadOnlySpan<char> line, out ReadOnlySpan<char> mark)
	{
		int spaces = CountLeading(line, ' ');

		mark = line[spaces..];

		// Four spaces or a tab make a line of code.
		return spaces <= MaxMarkIndent && mark is not ['\t', ..];
	}
	#endregion
}
