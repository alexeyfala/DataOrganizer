using DataOrganizer.GuardTests.TestHelpers.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// Reads the comments and literals of a C# source, the interpolated strings with their text and their holes.
/// </summary>
internal static class CodeLiteralReader
{
	#region Methods
	/// <summary>
	/// Reads the code of a source: each comment and literal becomes a space, and of an interpolated string only the code
	/// of its holes stays, each hole in its braces.
	/// </summary>
	public static string ReadCode(string text)
	{
		StringBuilder code = new();

		int index = 0;

		ReadCodeAt(text, ref index, code, [], isHole: false);

		return code.ToString();
	}

	/// <summary>
	/// Reads the interpolated strings of a source, those in the holes of others included.
	/// </summary>
	public static IEnumerable<InterpolatedString> ReadInterpolatedStrings(string text)
	{
		List<InterpolatedString> strings = [];

		int index = 0;

		ReadCodeAt(text, ref index, new StringBuilder(), strings, isHole: false);

		return strings;
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Counts the characters equal to a value that stand in a row from an index.
	/// </summary>
	private static int CountRun(string text, int index, char value)
	{
		int length = text
			.AsSpan(index)
			.IndexOfAnyExcept(value);

		return length < 0 ? text.Length - index : length;
	}

	/// <summary>
	/// Finds the closing quote of the character literal that a quote at an index opens, or <c>null</c> when it opens none.
	/// </summary>
	private static int? FindCharacterEnd(string text, int index)
	{
		int end = index + 1 < text.Length && text[index + 1] == '\\'
			? text.IndexOf('\'', Math.Min(index + 3, text.Length))
			: index + 2;

		return end > index && end < text.Length && text[end] == '\'' ? end : null;
	}

	/// <summary>
	/// Finds the end of the comment that starts at an index: the end of its line, or the index after its closing mark.
	/// </summary>
	private static int FindCommentEnd(string text, int index)
	{
		if (text[index + 1] == '/')
		{
			int length = text
				.AsSpan(index)
				.IndexOfAny('\r', '\n');

			return length < 0 ? text.Length : index + length;
		}

		int close = text.IndexOf("*/", index + 2, StringComparison.Ordinal);

		return close < 0 ? text.Length : close + 2;
	}

	/// <summary>
	/// Tells whether a string literal starts at an index: a quote, alone or after its <c>$</c> and <c>@</c> prefix.
	/// </summary>
	private static bool IsStringStart(string text, int index)
	{
		int quote = index;

		while (quote < text.Length && text[quote] is '$' or '@')
		{
			quote++;
		}

		return quote < text.Length && text[quote] == '"';
	}

	/// <summary>
	/// Reads code from an index to the end of the source or, in a hole, to the brace or the colon that ends its
	/// expression; comments and literals go into the code as spaces.
	/// </summary>
	private static void ReadCodeAt(string text, ref int index, StringBuilder code, List<InterpolatedString> strings, bool isHole)
	{
		int depth = 0;

		while (index < text.Length)
		{
			char current = text[index];

			char next = index + 1 < text.Length ? text[index + 1] : '\0';

			if (current == '/' && next is '/' or '*')
			{
				index = FindCommentEnd(text, index);

				code.Append(' ');

				continue;
			}

			if (current == '\'' && FindCharacterEnd(text, index) is { } end)
			{
				index = end + 1;

				code.Append(' ');

				continue;
			}

			if (IsStringStart(text, index))
			{
				ReadStringAt(text, ref index, code, strings);

				continue;
			}

			if (isHole && depth == 0 && (current == '}' || (current == ':' && next != ':')))
			{
				return;
			}

			depth += current switch
			{
				'(' or '[' or '{' => 1,
				')' or ']' or '}' => -1,
				_ => 0
			};

			int length = current == ':' && next == ':' ? 2 : 1;

			code.Append(text, index, length);

			index += length;
		}
	}

	/// <summary>
	/// Reads the hole of an interpolated string from the index after its opening braces to the index after its closing
	/// ones, its format left out, and returns its code.
	/// </summary>
	private static string ReadHoleAt(string text, ref int index, List<InterpolatedString> strings, int braces)
	{
		StringBuilder code = new();

		ReadCodeAt(text, ref index, code, strings, isHole: true);

		int close = text.IndexOf('}', index);

		index = close < 0 ? text.Length : close + Math.Min(CountRun(text, close, '}'), braces);

		return code.ToString();
	}

	/// <summary>
	/// Reads the string literal that starts at an index. A plain one goes into the code as a space, an interpolated one
	/// as the code of its holes and into the strings.
	/// </summary>
	private static void ReadStringAt(string text, ref int index, StringBuilder code, List<InterpolatedString> strings)
	{
		int start = index;

		index = text.IndexOf('"', start);

		ReadOnlySpan<char> prefix = text.AsSpan(start, index - start);

		bool isInterpolated = prefix.Contains('$');

		bool isVerbatim = prefix.Contains('@');

		int quotes = CountRun(text, index, '"');

		bool isRaw = !isVerbatim && quotes >= 3;

		int delimiter = isRaw ? quotes : 1;

		int braces = isRaw ? prefix.Count('$') : 1;

		List<string> holes = [];

		List<string> texts = [];

		StringBuilder piece = new();

		index += delimiter;

		while (index < text.Length)
		{
			char current = text[index];

			char next = index + 1 < text.Length ? text[index + 1] : '\0';

			if (current == '"' && (isRaw ? CountRun(text, index, '"') >= delimiter : !(isVerbatim && next == '"')))
			{
				index += delimiter;

				break;
			}

			if (!isRaw && !isVerbatim && current is '\r' or '\n')
			{
				break;
			}

			if (isInterpolated && current == '{' && (isRaw ? CountRun(text, index, '{') >= braces : next != '{'))
			{
				int run = CountRun(text, index, '{');

				piece.Append('{', run - braces);

				index += run;

				texts.Add(piece.ToString());

				piece.Clear();

				holes.Add(ReadHoleAt(text, ref index, strings, braces));

				continue;
			}

			if (!isRaw && (current == '"' || (isInterpolated && current is '{' or '}' && next == current)))
			{
				// A doubled quote of a verbatim string or a doubled brace of an interpolated one
				piece.Append(current);

				index += 2;

				continue;
			}

			if (!isRaw && !isVerbatim && current == '\\' && index + 1 < text.Length)
			{
				piece.Append(next == '"' ? "\"" : $"\\{next}");

				index += 2;

				continue;
			}

			piece.Append(current);

			index++;
		}

		texts.Add(piece.ToString());

		code.Append(' ');

		if (!isInterpolated)
		{
			return;
		}

		strings.Add(new()
		{
			Holes = [.. holes],
			Index = start,
			Texts = [.. texts],
			Value = text[start..index]
		});

		foreach (string hole in holes)
		{
			code
				.Append('{')
				.Append(hole)
				.Append("} ");
		}
	}
	#endregion
}
