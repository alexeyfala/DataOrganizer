using FileContents.Enums;

namespace FileContents.Dto;

/// <summary>
/// Outcome of reading the text of a file.
/// </summary>
public sealed record FileTextReading
{
	#region Properties
	/// <inheritdoc cref="FileTextStatus" />
	public required FileTextStatus Status { get; init; }

	/// <summary>
	/// Text of the file; empty unless <see cref="Status" /> is <see cref="FileTextStatus.Text" />.
	/// </summary>
	public string Text { get; init; } = string.Empty;
	#endregion
}
