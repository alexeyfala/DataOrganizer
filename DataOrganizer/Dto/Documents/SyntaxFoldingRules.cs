using System.Text.RegularExpressions;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Rules of a language for folding its text, taken from the settings of the language.
/// </summary>
public sealed record SyntaxFoldingRules
{
	#region Properties
	/// <summary>
	/// Token that closes a block comment; <c>null</c> when the language has no block comments.
	/// </summary>
	public string? BlockCommentEnd { get; init; }

	/// <summary>
	/// Token that opens a block comment; <c>null</c> when the language has no block comments.
	/// </summary>
	public string? BlockCommentStart { get; init; }

	/// <summary>
	/// Pattern of a line that holds a directive to the compiler, such as a condition of the preprocessor or an attribute;
	/// <c>null</c> when the language has no such lines.
	/// </summary>
	public Regex? DirectiveLine { get; init; }

	/// <summary>
	/// Pattern of the line that closes a marked block; <c>null</c> when the language marks no blocks.
	/// </summary>
	public Regex? EndMarker { get; init; }

	/// <summary>
	/// Pattern of a line that starts an import statement, such as a using directive; <c>null</c> when the imports of the
	/// language do not fold as a run.
	/// </summary>
	public Regex? ImportLine { get; init; }

	/// <summary>
	/// <c>True</c> for a language of the off-side rule, whose blocks leave the blank lines after them to the next block.
	/// </summary>
	public bool IsOffSide { get; init; }

	/// <summary>
	/// Pattern of the token that opens a line comment, matched at the start of a text; <c>null</c> when the language has
	/// no line comments.
	/// </summary>
	public Regex? LineComment { get; init; }

	/// <summary>
	/// Pattern of the line that opens a marked block; <c>null</c> when the language marks no blocks.
	/// </summary>
	public Regex? StartMarker { get; init; }
	#endregion
}
