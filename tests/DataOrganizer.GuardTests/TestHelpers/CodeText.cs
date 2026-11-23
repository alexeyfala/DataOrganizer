using System;
using System.Text.RegularExpressions;

namespace DataOrganizer.GuardTests.TestHelpers;

/// <summary>
/// The text of a C# file as a guard reads it: the code alone, without what only looks like code.
/// </summary>
internal static partial class CodeText
{
	#region Methods
	/// <summary>
	/// Finds the bracket that closes the round, square or curly one at an index, or the last index when none does.
	/// </summary>
	/// <exception cref="ArgumentException">The character at the index is no opening bracket.</exception>
	public static int FindClosing(string code, int open)
	{
		char opening = code[open];

		char closing = opening switch
		{
			'(' => ')',
			'[' => ']',
			'{' => '}',
			_ => throw new ArgumentException($"No opening bracket at {open}.", nameof(open))
		};

		int depth = 0;

		for (int i = open; i < code.Length; i++)
		{
			if (code[i] == opening)
			{
				depth++;
			}
			else if (code[i] == closing)
			{
				depth--;
			}

			if (depth == 0)
			{
				return i;
			}
		}

		return code.Length - 1;
	}

	/// <summary>
	/// Finds the end of the statement that starts at an index: its semicolon, or the end of the enclosing block.
	/// </summary>
	public static int FindStatementEnd(string code, int start)
	{
		int depth = 0;

		for (int i = start; i < code.Length; i++)
		{
			depth += code[i] switch
			{
				'(' or '{' => 1,
				')' or '}' => -1,
				_ => 0
			};

			if (depth < 0 || (depth == 0 && code[i] == ';'))
			{
				return i;
			}
		}

		return code.Length;
	}

	/// <summary>
	/// Returns the text without its comments, character literals and strings. An interpolated string stays whole,
	/// because what its holes hold is code.
	/// </summary>
	public static string WithoutCommentsAndLiterals(string text)
	{
		return CommentOrLiteralRegex().Replace(text, static x => x.Groups["interpolated"].Success ? x.Value : " ");
	}
	#endregion

	#region Helpers
	/// <summary>
	/// Matches the comments and literals of a C# file. An interpolated string is matched too, so that a quote inside it
	/// starts no other literal.
	/// </summary>
	[GeneratedRegex(
		"""
		(?<interpolated>\$+"{3,}[\s\S]*?"{3,}|(?:\$@|@\$)"(?:[^"]|"")*"|\$"(?:\\.|[^"\\\r\n])*")
		|"{3,}[\s\S]*?"{3,}
		|@"(?:[^"]|"")*"
		|"(?:\\.|[^"\\\r\n])*"
		|'(?:\\.|[^'\\\r\n])'
		|//[^\r\n]*
		|/\*[\s\S]*?\*/
		""",
		RegexOptions.IgnorePatternWhitespace)]
	private static partial Regex CommentOrLiteralRegex();
	#endregion
}
