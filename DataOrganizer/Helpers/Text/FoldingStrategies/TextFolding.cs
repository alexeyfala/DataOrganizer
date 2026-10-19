namespace DataOrganizer.Helpers.Text.FoldingStrategies;

/// <summary>
/// Block of a text that folds, by the offsets of its bounds.
/// </summary>
internal sealed record TextFolding
{
	#region Properties
	/// <summary>
	/// Offset where the folded text ends.
	/// </summary>
	public required int EndOffset { get; init; }

	/// <summary>
	/// Text shown in place of the folded block; <c>null</c> for the usual mark.
	/// </summary>
	public string? Name { get; init; }

	/// <summary>
	/// Offset where the folded text starts.
	/// </summary>
	public required int StartOffset { get; init; }
	#endregion
}
