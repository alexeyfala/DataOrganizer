namespace DataOrganizer.Dto.Documents;

/// <summary>
/// Position of the caret in a text.
/// </summary>
public readonly record struct CaretPosition
{
	#region Properties
	/// <summary>
	/// Column of the caret, counted from one.
	/// </summary>
	public int Column { get; init; }

	/// <summary>
	/// <c>True</c> when the caret stands at the end of a wrapped line, not at the start of the line that follows.
	/// </summary>
	public bool IsAtEndOfLine { get; init; }

	/// <summary>
	/// Line of the caret, counted from one.
	/// </summary>
	public int Line { get; init; }

	/// <summary>
	/// Column of the caret as shown, where a tab counts by its width; <c>-1</c> when it is not known.
	/// </summary>
	public int VisualColumn { get; init; }
	#endregion
}
