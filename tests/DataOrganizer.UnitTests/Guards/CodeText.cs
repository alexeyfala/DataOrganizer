using System.Text.RegularExpressions;

namespace DataOrganizer.UnitTests.Guards;

/// <summary>
/// The text of a C# file as a guard reads it: the code alone, without what only looks like code.
/// </summary>
internal static class CodeText
{
	#region Data
	/// <summary>
	/// Comments and literals of a C# file. An interpolated string is matched too, so that a quote inside it starts no
	/// other literal.
	/// </summary>
	private static readonly Regex CommentOrLiteral = new(
		"""
		(?<interpolated>\$+"{3,}[\s\S]*?"{3,}|(?:\$@|@\$)"(?:[^"]|"")*"|\$"(?:\\.|[^"\\\r\n])*")
		|"{3,}[\s\S]*?"{3,}
		|@"(?:[^"]|"")*"
		|"(?:\\.|[^"\\\r\n])*"
		|'(?:\\.|[^'\\\r\n])'
		|//[^\r\n]*
		|/\*[\s\S]*?\*/
		""",
		RegexOptions.IgnorePatternWhitespace);
	#endregion

	#region Methods
	/// <summary>
	/// Returns the text without its comments, character literals and strings. An interpolated string stays whole,
	/// because what its holes hold is code.
	/// </summary>
	public static string WithoutCommentsAndLiterals(string text)
	{
		return CommentOrLiteral.Replace(text, static x => x.Groups["interpolated"].Success ? x.Value : " ");
	}
	#endregion
}
