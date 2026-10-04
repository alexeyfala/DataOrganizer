using System.Collections.Generic;

namespace DataOrganizer.Dto.Documents;

/// <summary>
/// An item of a list with a search, such as a language of the syntax highlighting or an encoding of a text.
/// </summary>
public sealed record SelectorChoice
{
	#region Properties
	/// <summary>
	/// Mark shown after the item taken when none is chosen; <c>null</c> for the other items.
	/// </summary>
	public string? DefaultMark { get; init; }

	/// <summary>
	/// Text shown after the name in a lighter color; <c>null</c> for none.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Id of the item; <c>null</c> for the item that stands for none, as plain text does among the languages.
	/// </summary>
	public required string? Id { get; init; }

	/// <summary>
	/// <c>True</c> for the item taken when none is chosen.
	/// </summary>
	public bool IsDefault => DefaultMark is not null;

	/// <summary>
	/// Name of the item.
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Words besides the name that find the item, such as the extensions of a language without their dots.
	/// </summary>
	public required IReadOnlyList<string> SearchTerms { get; init; }
	#endregion
}
