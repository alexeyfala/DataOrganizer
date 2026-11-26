namespace DataOrganizer.GuardTests.TestHelpers.Dto;

/// <summary>
/// An interpolated string of a C# source: its place, the pieces of its text and the code of its holes.
/// </summary>
internal sealed record InterpolatedString
{
	#region Properties
	/// <summary>
	/// Code of each hole, its comments and literals left out.
	/// </summary>
	public required string[] Holes { get; init; }

	/// <summary>
	/// Index of the string in the source.
	/// </summary>
	public required int Index { get; init; }

	/// <summary>
	/// Pieces of the text before, between and after the holes, their escaped quotes and braces unescaped.
	/// </summary>
	public required string[] Texts { get; init; }

	/// <summary>
	/// The string as the source writes it.
	/// </summary>
	public required string Value { get; init; }
	#endregion
}
