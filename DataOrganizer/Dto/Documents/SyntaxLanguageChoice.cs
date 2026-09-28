using System.Collections.Generic;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// A language offered for the syntax highlighting of a text.
/// </summary>
public sealed record SyntaxLanguageChoice
{
	#region Properties
	/// <summary>
	/// Extensions of the files in the language, each with its dot.
	/// </summary>
	public required IReadOnlyList<string> Extensions { get; init; }

	/// <summary>
	/// Id of the language; <c>null</c> for plain text.
	/// </summary>
	public required string? Id { get; init; }

	/// <summary>
	/// <c>True</c> for the language that the text takes when none is chosen.
	/// </summary>
	public bool IsDefault { get; init; }

	/// <summary>
	/// Name of the language.
	/// </summary>
	public required string Name { get; init; }
	#endregion
}
