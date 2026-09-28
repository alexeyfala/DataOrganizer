using System.Text.RegularExpressions;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Rules of a language for folding its text, taken from the settings of the language.
/// </summary>
public sealed record SyntaxFoldingRules
{
	#region Properties
	/// <summary>
	/// Pattern of the line that closes a marked block; <c>null</c> when the language marks no blocks.
	/// </summary>
	public Regex? EndMarker { get; init; }

	/// <summary>
	/// <c>True</c> for a language of the off-side rule, whose blocks leave the blank lines after them to the next block.
	/// </summary>
	public bool IsOffSide { get; init; }

	/// <summary>
	/// Pattern of the line that opens a marked block; <c>null</c> when the language marks no blocks.
	/// </summary>
	public Regex? StartMarker { get; init; }
	#endregion
}
